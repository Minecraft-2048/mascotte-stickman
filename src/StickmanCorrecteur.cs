// Le correcteur du stickman (désactivé par défaut) : il repère un mot mal écrit dans le champ de texte
// où l'on tape, et le stickman vient le corriger.
//
// Ce que fait ce fichier, et ce qu'il ne fait pas :
// - il n'écoute PAS le clavier. Trois fois par seconde, il demande à Windows (UI Automation, l'interface
//   des lecteurs d'écran) les quelques dizaines de caractères qui précèdent le curseur de texte ;
// - il ne lit jamais un champ de mot de passe, et ne garde rien : ni fichier, ni réseau, juste le
//   dernier mot en mémoire le temps de le vérifier ;
// - l'orthographe est celle du correcteur de Windows (ISpellChecker), dans la langue de Windows ;
// - pour corriger, il sélectionne le mot fautif, tape le bon à la place, puis remet le curseur où il était.
// Tous les programmes ne donnent pas accès à leur texte : dans ceux-là, il ne se passe rien.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Text;

namespace MascotteStickman
{
    static class Correcteur
    {
        public sealed class Faute
        {
            public string Mot, Correction;
            public Rect Zone;                        // où le mot s'affiche, en pixels d'écran
            internal TextPatternRange Plage;         // le mot dans son texte
            internal AutomationElement Champ;
            internal double Trouvee;
        }

        public static Action<Faute> Trouvee;         // appelées depuis le fil du correcteur : à renvoyer vers l'interface
        public static Action<Faute, bool> Appliquee;

        static Thread fil;
        static volatile bool actif;
        static volatile Faute enCours, aAppliquer;
        static readonly Stopwatch montre = Stopwatch.StartNew();
        static readonly int moi = Process.GetCurrentProcess().Id;
        static string dernier;                       // le dernier bout de texte examiné : on ne le regarde qu'une fois

        public static void Regler(bool marche)
        {
            actif = marche;
            if (!marche || fil != null) return;
            fil = new Thread(Boucle) { IsBackground = true, Name = "Correcteur" };
            fil.SetApartmentState(ApartmentState.MTA);
            fil.Start();
        }

        // Le stickman est arrivé sur le mot : on corrige. La réponse revient par Appliquee.
        public static void Appliquer(Faute f) { aAppliquer = f; }

        // Le stickman a renoncé (attrapé, dérangé…) : on peut chercher la faute suivante.
        public static void Terminer() { enCours = null; aAppliquer = null; }

        static void Boucle()
        {
            ISpellChecker verificateur = null;
            bool cherche = false;
            while (true)
            {
                Thread.Sleep(actif ? 330 : 1000);
                if (!actif) { enCours = null; aAppliquer = null; continue; }
                try
                {
                    if (!cherche) { cherche = true; verificateur = Ouvrir(); }
                    if (verificateur == null) continue;          // pas de correcteur Windows pour cette langue
                    Faute f = aAppliquer;
                    if (f != null)
                    {
                        aAppliquer = null;
                        bool ok = false;
                        try { ok = Remplacer(f); }
                        catch (Exception) { }
                        enCours = null;
                        if (Appliquee != null) Appliquee(f, ok);
                        continue;
                    }
                    if (enCours != null)
                    {
                        if (montre.Elapsed.TotalSeconds - enCours.Trouvee > 14) enCours = null;      // le stickman n'est jamais venu
                        continue;
                    }
                    f = Observer(verificateur);
                    if (f == null) continue;
                    enCours = f;
                    if (Trouvee != null) Trouvee(f);
                }
                catch (Exception) { }                            // un programme qui répond mal à UI Automation : on réessaiera
            }
        }

        static ISpellChecker Ouvrir()
        {
            try
            {
                var fabrique = (ISpellCheckerFactory)new SpellCheckerFactory();
                foreach (string langue in new[] { CultureInfo.CurrentUICulture.Name, CultureInfo.CurrentCulture.Name, "fr-FR", "en-US" })
                {
                    int oui;
                    ISpellChecker v;
                    if (fabrique.IsSupported(langue, out oui) == 0 && oui != 0 && fabrique.CreateSpellChecker(langue, out v) == 0) return v;
                }
            }
            catch (Exception) { }                                // Windows 7, service absent…
            return null;
        }

        static bool Separateur(char c) { return char.IsWhiteSpace(c) || ".,;:!?…()[]\"«»".IndexOf(c) >= 0; }
        static bool DeMot(char c) { return char.IsLetter(c) || c == '\'' || c == '’' || c == '-'; }

        // Le champ où l'on tape, s'il veut bien montrer son texte (et n'est pas un mot de passe).
        static TextPattern Texte(out AutomationElement champ)
        {
            champ = AutomationElement.FocusedElement;
            if (champ == null || champ.Current.ProcessId == moi || champ.Current.IsPassword) return null;
            object motif;
            return champ.TryGetCurrentPattern(TextPattern.Pattern, out motif) ? (TextPattern)motif : null;
        }

        // Le curseur de texte, s'il n'y a pas de sélection en cours.
        static TextPatternRange Curseur(TextPattern texte)
        {
            TextPatternRange[] selection = texte.GetSelection();
            return selection.Length == 1 && selection[0].GetText(1).Length == 0 ? selection[0] : null;
        }

        // Y a-t-il, juste avant le curseur, un mot terminé (suivi d'une espace ou d'une ponctuation) et mal écrit ?
        static Faute Observer(ISpellChecker verificateur)
        {
            AutomationElement champ;
            TextPattern texte = Texte(out champ);
            if (texte == null) return null;
            TextPatternRange curseur = Curseur(texte);
            if (curseur == null) return null;

            const int Recul = 48;
            TextPatternRange avant = curseur.Clone();
            avant.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, -Recul);
            string t = avant.GetText(Recul + 8);
            if (t == dernier) return null;
            dernier = t;

            // les derniers mots terminés avant le curseur, du plus récent au plus ancien (on tape parfois
            // plus vite qu'il ne lit : il regarde jusqu'à trois mots en arrière)
            int fin = t.Length;
            while (fin > 0 && Separateur(t[fin - 1])) fin--;
            if (fin == t.Length || fin == 0) return null;         // pas encore fini d'écrire le mot en cours
            for (int examines = 0; examines < 3 && fin > 0; examines++)
            {
                int debut = fin;
                while (debut > 0 && DeMot(t[debut - 1])) debut--;
                if (debut == fin) break;                           // autre chose qu'un mot : chiffre, symbole…
                if (debut == 0 && t.Length >= Recul) break;         // mot coupé par le début de la fenêtre de lecture
                Faute f = Examiner(verificateur, t, debut, fin, curseur, champ);
                if (f != null) return f;
                fin = debut;
                while (fin > 0 && Separateur(t[fin - 1])) fin--;
            }
            return null;
        }

        static readonly Queue<string> vus = new Queue<string>();      // les mots déjà relus, avec ce qui les précède

        // Le mot t[debut..fin[ est-il mal écrit ? Chaque mot n'est relu qu'une fois.
        static Faute Examiner(ISpellChecker verificateur, string t, int debut, int fin, TextPatternRange curseur, AutomationElement champ)
        {
            string cle = t.Substring(Math.Max(0, fin - 28), fin - Math.Max(0, fin - 28));
            if (vus.Contains(cle)) return null;
            vus.Enqueue(cle);
            if (vus.Count > 40) vus.Dequeue();

            int apostrophe = Math.Max(t.LastIndexOf('\'', fin - 1, fin - debut), t.LastIndexOf('’', fin - 1, fin - debut));
            if (apostrophe >= debut) debut = apostrophe + 1;      // « l'ordinatuer » : on ne regarde que « ordinatuer »
            string mot = t.Substring(debut, fin - debut);
            if (mot.StartsWith("-") || mot.EndsWith("-")) return null;      // tiret de liste, mot coupé en fin de ligne…
            // ni les mots très courts, ni ceux qui ont une majuscule (noms propres, sigles) : trop de fausses alertes
            if (mot.Length < 4 || mot != mot.ToLower(CultureInfo.CurrentCulture)) return null;

            IEnumSpellingError erreurs;
            object erreur;
            if (verificateur.Check(mot, out erreurs) != 0 || erreurs.Next(out erreur) != 0 || erreur == null) return null;
            IEnumString propositions;
            var premiere = new string[1];
            if (verificateur.Suggest(mot, out propositions) != 0 || propositions.Next(1, premiere, IntPtr.Zero) != 0) return null;
            if (string.IsNullOrEmpty(premiere[0]) || premiere[0] == mot) return null;

            TextPatternRange plage = curseur.Clone();
            plage.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, -(t.Length - debut));
            plage.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, -(t.Length - fin));
            if (plage.GetText(-1) != mot) return null;            // ce programme compte ses caractères autrement : on s'abstient
            Rect[] zones = plage.GetBoundingRectangles();
            if (zones.Length == 0 || zones[0].IsEmpty || zones[0].Width < 2) return null;      // mot hors de l'écran

            return new Faute { Mot = mot, Correction = premiere[0], Zone = zones[0], Plage = plage, Champ = champ, Trouvee = montre.Elapsed.TotalSeconds };
        }

        // Ce qui a été écrit depuis le mot : de sa fin jusqu'au curseur.
        static string Depuis(Faute f, TextPatternRange curseur)
        {
            TextPatternRange suite = f.Plage.Clone();
            suite.MoveEndpointByRange(TextPatternRangeEndpoint.Start, f.Plage, TextPatternRangeEndpoint.End);
            suite.MoveEndpointByRange(TextPatternRangeEndpoint.End, curseur, TextPatternRangeEndpoint.Start);
            return suite.GetText(400);
        }

        static bool Remplacer(Faute f)
        {
            string suite = null;
            for (int essai = 0; ; essai++)                         // on attend qu'il s'arrête d'écrire un instant
            {
                AutomationElement champ;
                TextPattern texte = Texte(out champ);
                if (texte == null || !Automation.Compare(champ, f.Champ)) return false;       // il est passé à autre chose
                if (f.Plage.GetText(-1) != f.Mot) return false;                               // le mot a déjà été retouché
                TextPatternRange curseur = Curseur(texte);
                if (curseur == null) return false;
                string encore = Depuis(f, curseur);
                if (encore.Length > 300) return false;                                        // trop loin derrière lui
                if (encore == suite) break;
                if (essai >= 6) return false;
                suite = encore;
                Thread.Sleep(220);
            }
            int retour = suite.Replace("\r\n", "\n").Length;       // de combien remettre le curseur vers la droite
            f.Plage.Select();
            Thread.Sleep(70);
            Taper(f.Correction, retour);
            return true;
        }

        // La correction tapée d'un seul bloc (Windows ne laisse rien s'intercaler), puis le curseur remis à sa place.
        static void Taper(string texte, int versLaDroite)
        {
            var touches = new List<INPUT>();
            foreach (char c in texte)
            {
                touches.Add(Touche(0, c, 0x0004));
                touches.Add(Touche(0, c, 0x0004 | 0x0002));
            }
            for (int i = 0; i < versLaDroite; i++)
            {
                touches.Add(Touche(0x27, '\0', 0x0001));
                touches.Add(Touche(0x27, '\0', 0x0001 | 0x0002));
            }
            SendInput((uint)touches.Count, touches.ToArray(), Marshal.SizeOf(typeof(INPUT)));
        }

        static INPUT Touche(ushort virtuelle, char caractere, uint drapeaux)
        {
            var i = new INPUT { type = 1 };
            i.u.ki = new KEYBDINPUT { wVk = virtuelle, wScan = caractere, dwFlags = drapeaux };
            return i;
        }

        // ------------------------------------------------------------ Windows

        [StructLayout(LayoutKind.Sequential)] struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)] struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Explicit)] struct UNION { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; }
        [StructLayout(LayoutKind.Sequential)] struct INPUT { public uint type; public UNION u; }
        [DllImport("user32.dll")] static extern uint SendInput(uint nombre, INPUT[] touches, int taille);

        [ComImport, Guid("7AB36653-1796-484B-BDFA-E74F1DB7C1DC")]
        class SpellCheckerFactory { }

        [ComImport, Guid("8E018A9D-2415-4677-BF08-794EA61F94BB"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface ISpellCheckerFactory
        {
            [PreserveSig] int get_SupportedLanguages(out IEnumString langues);
            [PreserveSig] int IsSupported([MarshalAs(UnmanagedType.LPWStr)] string langue, out int oui);
            [PreserveSig] int CreateSpellChecker([MarshalAs(UnmanagedType.LPWStr)] string langue, out ISpellChecker verificateur);
        }

        [ComImport, Guid("B6FD0B71-E2BC-4653-8D05-F197E412770B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface ISpellChecker
        {
            [PreserveSig] int get_LanguageTag([MarshalAs(UnmanagedType.LPWStr)] out string langue);
            [PreserveSig] int Check([MarshalAs(UnmanagedType.LPWStr)] string texte, out IEnumSpellingError erreurs);
            [PreserveSig] int Suggest([MarshalAs(UnmanagedType.LPWStr)] string mot, out IEnumString propositions);
        }

        [ComImport, Guid("803E3BD4-2828-4410-8290-418D1D73C762"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IEnumSpellingError
        {
            [PreserveSig] int Next([MarshalAs(UnmanagedType.IUnknown)] out object erreur);
        }
    }
}
