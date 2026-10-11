// « Passer les pubs » (désactivé par défaut) : quand YouTube affiche son bouton « Ignorer l'annonce », un
// stickman vole jusqu'à lui et appuie dessus. Rien n'est bloqué ni masqué : c'est le bouton que YouTube
// propose lui-même, pressé dès qu'il apparaît, comme on le ferait à la souris.
//
// Comme le correcteur, il passe par UI Automation (ce que lisent les lecteurs d'écran), et seulement dans les
// fenêtres de navigateur dont le titre contient « YouTube », au premier plan ou derrière : il y cherche un
// bouton qui porte l'un des noms connus du bouton d'annonce, et vérifie que c'est bien lui (son identifiant ou
// sa classe contient « skip »). Il ne lit rien d'autre de la page, ne garde rien, n'envoie rien. Si le bouton
// est caché par une autre fenêtre, il est pressé sans qu'un stickman fasse le voyage.
// Limite, côté navigateur : une page entièrement recouverte, réduite, ou dans un onglet caché n'est plus mise à
// jour pour les outils d'accessibilité ; le bouton n'est alors vu que lorsque la page redevient visible, même
// un peu (essai fait avec Edge : pressé en arrière-plan tant qu'un bout de la fenêtre dépasse).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using Condition = System.Windows.Automation.Condition;      // System.Windows a aussi une classe Condition

namespace MascotteStickman
{
    static class Pubs
    {
        public static Action<Rect> Trouve;           // appelée depuis le fil des pubs : le bouton est là (pixels d'écran ; vide s'il ne se voit pas)

        static readonly string[] navigateurs = { "chrome", "msedge", "firefox", "brave", "opera", "vivaldi", "arc" };
        static readonly string[] noms =
        {
            "Skip", "Skip Ad", "Skip Ads", "Ignorer", "Ignorer l'annonce", "Ignorer l’annonce", "Ignorer les annonces",
            "Passer", "Passer l'annonce", "Passer l’annonce", "Passer les annonces",
        };
        static readonly Stopwatch montre = Stopwatch.StartNew();
        static Thread fil;
        static volatile bool actif, aPresser;
        static AutomationElement enCours;            // le bouton trouvé, en attendant que le stickman arrive
        static double trouveA, dernierClic = -10;
        static Condition condition;
        static IntPtr fenetreDuBouton;               // la fenêtre du navigateur où il a été trouvé
        static int pause = 700;                      // millisecondes entre deux recherches
        static bool visible;                         // le bouton trouvé se voit-il à l'écran ? (sinon on appuie sans y envoyer personne)
        static readonly Dictionary<uint, string> programmes = new Dictionary<uint, string>();      // nom du programme de chaque fenêtre déjà vue

        public static void Regler(bool marche)
        {
            actif = marche;
            if (!marche || fil != null) return;
            fil = new Thread(Boucle) { IsBackground = true, Name = "Pubs" };
            fil.SetApartmentState(ApartmentState.MTA);
            fil.Start();
        }

        // Le stickman est arrivé sur le bouton (ou personne n'était libre) : on appuie.
        public static void Presser() { aPresser = true; }

        static void Boucle()
        {
            while (true)
            {
                Thread.Sleep(actif ? (enCours != null ? 120 : pause) : 1500);
                if (!actif) { enCours = null; aPresser = false; continue; }
                try
                {
                    double t = montre.Elapsed.TotalSeconds;
                    if (enCours != null)
                    {
                        if (!aPresser && t - trouveA < 5) continue;      // le stickman est en route ; au bout de 5 s, on n'attend plus
                        aPresser = false;
                        AutomationElement bouton = enCours;
                        enCours = null;
                        dernierClic = t;
                        Appuyer(bouton, fenetreDuBouton);
                        continue;
                    }
                    aPresser = false;
                    if (t - dernierClic < 1.5) continue;
                    double debut = montre.Elapsed.TotalMilliseconds;
                    AutomationElement trouve = Chercher();
                    // une grosse page est longue à parcourir : on lui laisse au moins deux fois ce temps-là avant de recommencer
                    pause = (int)Math.Max(700, Math.Min(2500, 2 * (montre.Elapsed.TotalMilliseconds - debut)));
                    if (trouve == null) continue;
                    enCours = trouve; trouveA = t;
                    if (Trouve != null) Trouve(visible ? trouve.Current.BoundingRectangle : Rect.Empty);      // vide : le bouton est caché derrière une autre fenêtre
                }
                catch (Exception) { enCours = null; }            // navigateur qui répond mal à UI Automation : on réessaiera
            }
        }

        // Toutes les fenêtres de navigateur dont le titre contient « YouTube », pas seulement celle du premier plan :
        // la vidéo peut jouer derrière une autre fenêtre, ou dans une fenêtre réduite, pendant qu'on fait autre chose.
        // (Un onglet caché derrière un autre onglet de la même fenêtre reste hors de portée : le navigateur ne montre
        // à UI Automation que l'onglet affiché.)
        static AutomationElement Chercher()
        {
            var fenetres = new List<IntPtr>();
            EnumWindows((fenetre, parametre) =>                  // dans l'ordre de l'écran : celle du dessus d'abord
            {
                if (IsWindowVisible(fenetre) && EstYouTube(fenetre)) fenetres.Add(fenetre);
                return true;
            }, IntPtr.Zero);
            if (fenetres.Count == 0) return null;

            if (condition == null)
            {
                var parNom = new Condition[noms.Length];
                for (int i = 0; i < noms.Length; i++) parNom[i] = new PropertyCondition(AutomationElement.NameProperty, noms[i], PropertyConditionFlags.IgnoreCase);
                condition = new AndCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button), new OrCondition(parNom));
            }
            foreach (IntPtr fenetre in fenetres)
            {
                bool reduite = IsIconic(fenetre);
                AutomationElementCollection boutons;
                try { boutons = AutomationElement.FromHandle(fenetre).FindAll(TreeScope.Descendants, condition); }
                catch (Exception) { continue; }                  // cette fenêtre répond mal : on regarde les autres
                foreach (AutomationElement bouton in boutons)
                {
                    AutomationElement.AutomationElementInformation b = bouton.Current;
                    if (!b.IsEnabled) continue;
                    if (!reduite && (b.IsOffscreen || b.BoundingRectangle.IsEmpty || b.BoundingRectangle.Width < 8)) continue;
                    // un bouton « Ignorer » peut exister ailleurs : celui de l'annonce se reconnaît à son identifiant ou à sa classe
                    string marque = (b.AutomationId ?? "") + " " + (b.ClassName ?? "");
                    if (marque.IndexOf("skip", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    visible = !reduite && ALEcran(fenetre, b.BoundingRectangle);
                    fenetreDuBouton = fenetre;
                    return bouton;
                }
            }
            return null;
        }

        static bool EstYouTube(IntPtr fenetre)
        {
            var titre = new StringBuilder(400);
            GetWindowText(fenetre, titre, titre.Capacity);
            if (titre.ToString().IndexOf("YouTube", StringComparison.OrdinalIgnoreCase) < 0) return false;
            uint pid;
            GetWindowThreadProcessId(fenetre, out pid);
            string programme;
            if (!programmes.TryGetValue(pid, out programme))
            {
                try { programme = Process.GetProcessById((int)pid).ProcessName.ToLowerInvariant(); }
                catch (ArgumentException) { return false; }
                if (programmes.Count > 200) programmes.Clear();
                programmes[pid] = programme;
            }
            return Array.IndexOf(navigateurs, programme) >= 0;
        }

        // Le bouton se voit-il vraiment ? (sa fenêtre peut être recouverte par une autre : inutile d'y envoyer un stickman)
        static bool ALEcran(IntPtr fenetre, Rect zone)
        {
            POINT p;
            p.X = (int)(zone.Left + zone.Width / 2); p.Y = (int)(zone.Top + zone.Height / 2);
            return GetAncestor(WindowFromPoint(p), 2) == fenetre;
        }

        // Presser un bouton d'une page fait passer le navigateur au premier plan. Si l'on travaillait dans une autre
        // fenêtre, on lui rend la main aussitôt, et le navigateur retourne derrière, là où il était.
        static void Appuyer(AutomationElement bouton, IntPtr navigateur)
        {
            object motif;
            if (!bouton.TryGetCurrentPattern(InvokePattern.Pattern, out motif)) return;
            IntPtr devant = GetForegroundWindow(), dessus = GetWindow(navigateur, 3);      // 3 : la fenêtre juste au-dessus de lui
            ((InvokePattern)motif).Invoke();
            if (devant == IntPtr.Zero || devant == navigateur) return;                     // YouTube était déjà devant : rien à rendre
            for (int i = 0; i < 75; i++)                                                   // une seconde et demie, pas plus
            {
                IntPtr maintenant = GetForegroundWindow();
                if (maintenant == navigateur)
                {
                    uint pid, moi = GetCurrentThreadId(), lui = GetWindowThreadProcessId(navigateur, out pid);
                    AttachThreadInput(moi, lui, true);                                     // Windows ne laisse changer de fenêtre que le fil qui a la main
                    SetForegroundWindow(devant);
                    AttachThreadInput(moi, lui, false);
                    if (dessus != IntPtr.Zero && IsWindow(dessus)) SetWindowPos(navigateur, dessus, 0, 0, 0, 0, 0x0013);      // sans le bouger, ni l'activer
                    return;
                }
                if (maintenant != devant) return;                                          // on est parti ailleurs de soi-même : on ne touche à rien
                Thread.Sleep(20);
            }
        }

        delegate bool RappelFenetre(IntPtr fenetre, IntPtr parametre);
        [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
        [DllImport("user32.dll")] static extern bool EnumWindows(RappelFenetre rappel, IntPtr parametre);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr fenetre);
        [DllImport("user32.dll")] static extern bool IsIconic(IntPtr fenetre);
        [DllImport("user32.dll")] static extern bool IsWindow(IntPtr fenetre);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr fenetre);
        [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr fenetre, uint lequel);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr fenetre, IntPtr apres, int x, int y, int l, int h, uint drapeaux);
        [DllImport("user32.dll")] static extern bool AttachThreadInput(uint fil, uint autre, bool attacher);
        [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(POINT point);
        [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr fenetre, uint lequel);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr fenetre, StringBuilder texte, int taille);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr fenetre, out uint pid);
    }
}
