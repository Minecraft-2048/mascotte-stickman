// L'oreille du stickman : la meme que celle de la mascotte Jungkook (crete-metre de la sortie audio,
// tempo par autocorrelation). Elle ne fait que lire le niveau du son : rien n'est enregistre.
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace MascotteStickman
{
    // L'oreille : écoute le niveau de la sortie audio de Windows (crête-mètre Core Audio, sans rien
    // enregistrer), en déduit s'il y a de la musique, son tempo et la place des temps. Elle tourne sur
    // son propre fil et prévient la mascotte à chaque changement et à chaque demi-temps.
    sealed class Oreille
    {
        const double Seau = 0.02;                // un point d'enveloppe toutes les 20 ms
        const int Memoire = 300;                 // 6 s d'enveloppe
        const int RetardMin = 17, RetardMax = 50;   // périodes cherchées : de 176 à 60 battements par minute

        readonly float[] enveloppe = new float[Memoire];
        long remplis;                            // seaux remplis depuis le début (rangés en cercle)
        readonly Action<bool, double> changement;   // (musique ou pas, durée d'un temps en secondes)
        readonly Action<bool> demiTemps;         // vrai sur le temps, faux entre deux temps
        public volatile bool Active = true;

        bool musique;
        int oui, non;                            // analyses de suite qui disent « musique » ou « plus de musique »
        double periode = 0.5, prochainDemi, dernierDemi;
        bool prochainFort;

        public Oreille(Action<bool, double> changement, Action<bool> demiTemps)
        {
            this.changement = changement;
            this.demiTemps = demiTemps;
            var fil = new Thread(Boucle) { IsBackground = true, Name = "Oreille", Priority = ThreadPriority.AboveNormal };
            fil.Start();
        }

        void Boucle()
        {
            IAudioMeterInformation metre = null;
            var montre = Stopwatch.StartNew();
            double rouvrir = 0, analyse = 0;
            long seau = -1;
            float crete = 0;
            while (true)
            {
                Thread.Sleep(Active ? 8 : 400);
                double t = montre.Elapsed.TotalSeconds;
                if (!Active)
                {
                    if (musique) Annoncer(false);
                    remplis = 0;
                    seau = -1;
                    continue;
                }
                // la sortie par défaut peut changer (casque branché) : on la reprend de temps en temps
                if (metre == null || (t >= rouvrir && !musique))
                {
                    if (metre != null) Marshal.ReleaseComObject(metre);
                    metre = Ouvrir();
                    rouvrir = t + 10;
                    if (metre == null) { rouvrir = t + 5; Thread.Sleep(2000); continue; }
                }
                float valeur;
                if (metre.GetPeakValue(out valeur) != 0) { Marshal.ReleaseComObject(metre); metre = null; continue; }

                long s = (long)(t / Seau);
                if (s != seau)
                {
                    if (seau >= 0)
                        for (long k = seau; k < s && k < seau + Memoire; k++) Ajouter(crete);   // un seau sauté garde la valeur du précédent
                    seau = s;
                    crete = 0;
                }
                crete = Math.Max(crete, valeur);

                if (t >= analyse)
                {
                    analyse = t + 0.5;
                    Analyser(seau * Seau);
                }
                if (musique && t >= prochainDemi)
                {
                    dernierDemi = prochainDemi;
                    prochainDemi += periode / 2;
                    if (prochainDemi < t) prochainDemi = t + periode / 2;   // le fil a pris du retard : on ne rattrape pas
                    bool fort = prochainFort;
                    prochainFort = !prochainFort;
                    demiTemps(fort);
                }
            }
        }

        void Ajouter(float valeur)
        {
            enveloppe[remplis % Memoire] = valeur;
            remplis++;
        }

        // maintenant = début du seau en cours, qui n'est pas encore dans l'enveloppe
        void Analyser(double maintenant)
        {
            int n = (int)Math.Min(remplis, Memoire);
            if (n < 150) return;                                  // moins de 3 s d'écoute
            var x = new double[n];
            for (int i = 0; i < n; i++) x[i] = Math.Sqrt(enveloppe[(remplis - n + i) % Memoire]);

            // du son presque sans interruption pendant les 3 dernières secondes (une ambiance très
            // faible, le bruit de fond d'un jeu, reste sous le seuil : crête de 0,035)
            int sonores = 0;
            for (int i = n - 150; i < n; i++) if (x[i] > 0.187) sonores++;
            double continu = sonores / 150.0;

            // attaques : ce qui dépasse la moyenne des 60 ms précédentes
            var o = new double[n];
            double somme = 0;
            for (int i = 3; i < n; i++) { o[i] = Math.Max(0, x[i] - (x[i - 1] + x[i - 2] + x[i - 3]) / 3); somme += o[i]; }
            double moyenne = somme / n, energie = 0;
            for (int i = 0; i < n; i++) { o[i] -= moyenne; energie += o[i] * o[i]; }
            if (energie < 1e-6) { Decider(false, continu, 0, periode); return; }

            // tempo : autocorrélation des attaques, un peu favorisée autour de 110 battements par minute
            var ac = new double[RetardMax + 2];
            for (int r = RetardMin - 1; r <= RetardMax + 1; r++)
            {
                double a = 0;
                for (int i = r; i < n; i++) a += o[i] * o[i - r];
                ac[r] = a / energie;
            }
            int meilleur = RetardMin;
            double score = double.MinValue;
            for (int r = RetardMin; r <= RetardMax; r++)
            {
                double ecart = Math.Log(r / 27.0, 2) / 1.2;
                double note = ac[r] * Math.Exp(-0.5 * ecart * ecart);
                if (note > score) { score = note; meilleur = r; }
            }
            // une musique hésite souvent entre deux tempos (ici 94 et 141) : tant que celui du moment
            // tient presque aussi bien, on le garde, sinon la danse changerait de vitesse sans arrêt
            int actuel = (int)Math.Round(periode / Seau);
            if (musique && actuel > RetardMin && actuel < RetardMax)
            {
                int voisin = actuel;
                for (int r = actuel - 1; r <= actuel + 1; r++) if (ac[r] > ac[voisin]) voisin = r;
                double ecart = Math.Log(voisin / 27.0, 2) / 1.2;
                if (ac[voisin] * Math.Exp(-0.5 * ecart * ecart) >= 0.7 * score) meilleur = voisin;
            }
            double confiance = ac[meilleur];
            // période plus fine que le seau : sommet de la parabole qui passe par les trois points
            double gauche = ac[meilleur - 1], milieu = ac[meilleur], droite = ac[meilleur + 1];
            double courbure = gauche - 2 * milieu + droite;
            double p = meilleur + (courbure < 0 ? Math.Max(-0.5, Math.Min(0.5, 0.5 * (gauche - droite) / courbure)) : 0);

            bool dejaLa = musique;
            Decider(continu > 0.75 && confiance > 0.25, continu, confiance, p * Seau);
            if (!musique) return;

            // place des temps : le décalage qui fait tomber le plus d'attaques sur la grille de période p
            int pas = (int)Math.Round(p);
            int phase = 0;
            double meilleurePhase = double.MinValue;
            for (int f = 0; f < pas; f++)
            {
                double s = 0;
                for (double i = n - 1 - f; i >= 0; i -= p) s += o[(int)Math.Round(i)];
                if (s > meilleurePhase) { meilleurePhase = s; phase = f; }
            }
            double nouvellePeriode = p * Seau;
            double dernierTemps = maintenant - (phase + 0.5) * Seau;
            // le prochain demi-temps sur la nouvelle grille, sans en jouer deux presque à la suite
            double demi = nouvellePeriode / 2;
            double suivant = dernierTemps + Math.Ceiling((maintenant - dernierTemps) / demi) * demi;
            if (suivant - dernierDemi < demi * 0.5) suivant += demi;
            bool changeTempo = dejaLa && Math.Abs(nouvellePeriode - periode) > 0.03;
            periode = nouvellePeriode;
            prochainDemi = suivant;
            prochainFort = Math.Abs(((suivant - dernierTemps) / nouvellePeriode) - Math.Round((suivant - dernierTemps) / nouvellePeriode)) < 0.25;
            if (changeTempo) changement(true, periode);
            Trace("tempo " + (60 / periode).ToString("0") + " bpm, confiance " + confiance.ToString("0.00") + ", continu " + continu.ToString("0.00"));
        }

        // Il faut trois analyses de suite (1,5 s) pour commencer à danser, et quatre (2 s) pour s'arrêter.
        void Decider(bool entendue, double continu, double confiance, double nouvellePeriode)
        {
            if (entendue) { oui++; non = 0; } else { non++; oui = 0; }
            if (!musique && oui >= 3)
            {
                Trace("musique ! continu " + continu.ToString("0.00") + " confiance " + confiance.ToString("0.00"));
                periode = nouvellePeriode;
                Annoncer(true);
            }
            else if (musique && non >= 4 && continu < 0.6)
            {
                Trace("fin de la musique");
                Annoncer(false);
            }
        }

        void Annoncer(bool active)
        {
            musique = active;
            oui = non = 0;
            if (active) { dernierDemi = 0; prochainDemi = double.MaxValue; }
            changement(active, periode);
        }

        static IAudioMeterInformation Ouvrir()
        {
            try
            {
                var enumerateur = (IMMDeviceEnumerator)new MMDeviceEnumerator();
                IMMDevice appareil;
                if (enumerateur.GetDefaultAudioEndpoint(0, 1, out appareil) != 0) return null;   // sortie, multimédia
                Guid iid = typeof(IAudioMeterInformation).GUID;
                object metre;
                if (appareil.Activate(ref iid, 23, IntPtr.Zero, out metre) != 0) return null;
                return (IAudioMeterInformation)metre;
            }
            catch (Exception) { return null; }                   // pas de carte son, service audio arrêté…
        }

        static readonly bool traceActive = File.Exists(Path.Combine(Programme.Dossier, "trace.on"));

        public static void Trace(string texte)
        {
            if (!traceActive) return;
            try { File.AppendAllText(Path.Combine(Programme.Dossier, "trace.log"), DateTime.Now.ToString("HH:mm:ss.fff ") + texte + "\r\n"); }
            catch (IOException) { }
        }

        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        class MMDeviceEnumerator { }

        [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDeviceEnumerator
        {
            int EnumAudioEndpoints();                             // pas utilisée : simple place dans la table
            [PreserveSig] int GetDefaultAudioEndpoint(int flux, int role, out IMMDevice appareil);
        }

        [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDevice
        {
            [PreserveSig] int Activate(ref Guid iid, int contexte, IntPtr parametres, [MarshalAs(UnmanagedType.IUnknown)] out object obtenu);
        }

        [ComImport, Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IAudioMeterInformation
        {
            [PreserveSig] int GetPeakValue(out float crete);
        }
    }
}
