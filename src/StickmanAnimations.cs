// Bibliothèque d'animations du stickman.
// Une pose = 13 nombres (voir I) ; une animation = une fonction « phase 0..1 -> pose ».
// Les poses s'écrivent en texte, dans cet ordre :
//   torse tête | épaule1 coude1 épaule2 coude2 | hanche1 genou1 hanche2 genou2 | hauteur rotation décalage
// Angles en degrés, personnage tourné vers la droite : 0 = membre pendant vers le bas, 90 = vers l'avant,
// 180 = vers le haut. Un genou plié est négatif. Six nombres seulement = geste du haut du corps.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MascotteStickman
{
    static class I
    {
        public const int X = 0, Air = 1, Rot = 2, Torse = 3, Tete = 4, Ep1 = 5, Co1 = 6, Ep2 = 7, Co2 = 8,
            Ha1 = 9, Ge1 = 10, Ha2 = 11, Ge2 = 12, N = 13;
    }

    sealed class Anim
    {
        public string Nom, Famille, Objet;
        public double Duree = 1;                 // secondes par cycle
        public int Tours = 1;                    // cycles quand elle est tirée au hasard
        public bool Haut;                        // haut du corps seulement : se superpose à ce que font les jambes
        public bool Deplace;                     // déplacement : jouée jusqu'à destination
        public double Vitesse;                   // px/s vers l'avant (négatif : à reculons)
        public string Son;                       // bruitage joué une fois, quand l'animation atteint SonA
        public double SonA;                      // en fraction du premier cycle
        public bool SurFenetre;                  // n'a de sens que perché sur une fenêtre (jambes dans le vide…)
        public string Special;                   // pas une simple suite de poses : toute une scène (tour de blocs, élytres)
        public string[] Bruits;                  // plusieurs bruitages dans l'animation (les checks) : noms et instants
        public double[] BruitsA;
        public Func<double, double[]> Pose;
    }

    // Une animation à deux : celui qui arrive joue A, celui qui l'attend joue B, face à face et en même temps.
    sealed class Duo
    {
        public string Nom;
        public Anim A, B;                        // toutes deux nulles : ils dansent ensemble (une danse tirée au hasard)
        public double Distance;                  // écart entre eux, à taille 1
        public string DitA, DitB, FinA, FinB;    // bulles du début et de la fin
        public bool Hasard;                      // les rôles sont tirés au sort (pierre-feuille-ciseaux)
    }

    static class Biblio
    {
        public static readonly List<Anim> Toutes = new List<Anim>();
        public static readonly List<string> Familles = new List<string>();
        public static readonly List<Anim> Repos = new List<Anim>();
        public static readonly List<Duo> Duos = new List<Duo>();
        public static Anim Marche, Course, Reception, Heros, SeReleve, Dort, Salut, PoingHaut, Poing, PiedMoyen, PiedBas, Danse, Concentration, Apparition;
        public static Anim Appuie, Relache;
        public static Anim PoseBloc, PoseMarche, Admire, Elan, Lance, PoseDevant, Oreilles, Ramasse;
        public static Anim Clique, Pointe;
        public static Anim LeveBaton, TientBaton, Sursaut, Levitation, Creatif, AssisFeu, Appui, Tournoie, Rugit;
        public static double[] PoseElytres;
        public const double Bloc = 48;           // côté d'un bloc de Minecraft, à taille 1 : trois fois ses 16 pixels de texture
        public static double[] Groupe;
        public static double[] SautMonte, SautDescend;

        const string S = "0 0 10 12 -10 12 6 0 -6 0";                       // debout
        const string Cr = "22 5 -30 25 -40 25 62 -112 52 -104";             // accroupi
        const string G = "8 0 70 100 55 110 14 -15 -14 -10";                // en garde
        const string Boule = "20 20 60 80 55 85 105 -130 98 -125";          // groupé (saltos)
        const string Dos = "0 10 25 5 28 -5 15 0 12 0 0 -75";               // allongé sur le dos
        const string Ventre = "0 0 165 20 170 10 -15 0 -12 0 0 75";         // allongé sur le ventre
        const string Planche = "0 -10 62 0 64 0 0 0 2 0 0 62";              // en appui sur les mains

        static readonly int[] Ordre = { I.Torse, I.Tete, I.Ep1, I.Co1, I.Ep2, I.Co2, I.Ha1, I.Ge1, I.Ha2, I.Ge2, I.Air, I.Rot, I.X };

        // ------------------------------------------------------------ outils

        public static double[] Neutre() { return L(S); }

        public static double[] L(string texte)
        {
            var p = new double[I.N];
            p[I.Ha1] = 6; p[I.Ha2] = -6;
            string[] m = texte.Split(new[] { ' ', '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < m.Length; i++) p[Ordre[i]] = double.Parse(m[i], CultureInfo.InvariantCulture);
            return p;
        }

        static int Nombres(string texte) { return texte.Split(new[] { ' ', '|' }, StringSplitOptions.RemoveEmptyEntries).Length; }

        public static double[] Mix(double[] a, double[] b, double k)
        {
            var p = new double[I.N];
            for (int i = 0; i < I.N; i++) p[i] = a[i] + (b[i] - a[i]) * k;
            return p;
        }

        static double[] Echange(double[] p)
        {
            var q = (double[])p.Clone();
            q[I.Ep1] = p[I.Ep2]; q[I.Co1] = p[I.Co2]; q[I.Ep2] = p[I.Ep1]; q[I.Co2] = p[I.Co1];
            q[I.Ha1] = p[I.Ha2]; q[I.Ge1] = p[I.Ge2]; q[I.Ha2] = p[I.Ha1]; q[I.Ge2] = p[I.Ge1];
            return q;
        }

        public static Anim Trouver(string nom)
        {
            foreach (Anim a in Toutes) if (string.Equals(a.Nom, nom, StringComparison.OrdinalIgnoreCase)) return a;
            foreach (Anim a in Toutes) if (a.Nom.StartsWith(nom, StringComparison.OrdinalIgnoreCase)) return a;
            return null;
        }

        // famille null = animation interne (réception, se relever…) : ni tirée au hasard ni listée
        static Anim Ajouter(string famille, string nom, double duree, int tours, Func<double, double[]> pose)
        {
            var a = new Anim { Nom = nom, Famille = famille, Duree = duree, Tours = tours, Pose = pose };
            if (famille == null) return a;
            Toutes.Add(a);
            if (!Familles.Contains(famille)) Familles.Add(famille);
            return a;
        }

        // Va-et-vient régulier entre deux poses.
        static Anim Osc(string famille, string nom, string a, string b, double periode, int tours, string objet = null)
        {
            double[] pa = L(a), pb = L(b);
            Anim x = Ajouter(famille, nom, periode, tours, t => Mix(pa, pb, (1 - Math.Cos(2 * Math.PI * t)) / 2));
            x.Haut = Nombres(a) <= 6;
            x.Objet = objet;
            return x;
        }

        // Pose tenue, avec une légère respiration.
        static Anim Fixe(string famille, string nom, string a, int tours = 3, string objet = null)
        {
            double[] pa = L(a), pb = L(a);
            pb[I.Torse] += 2; pb[I.Tete] += 2;
            Anim x = Ajouter(famille, nom, 1.6, tours, t => Mix(pa, pb, (1 - Math.Cos(2 * Math.PI * t)) / 2));
            x.Haut = Nombres(a) <= 6;
            x.Objet = objet;
            return x;
        }

        // Suite de poses datées : "ms:pose;ms:pose;…", reliées par une courbe lisse.
        static Anim Cles(string famille, string nom, string texte, string objet = null, double vitesse = 0)
        {
            string[] morceaux = texte.Split(';');
            int n = morceaux.Length;
            var temps = new double[n];
            var cles = new double[n][];
            for (int i = 0; i < n; i++)
            {
                int deux = morceaux[i].IndexOf(':');
                temps[i] = double.Parse(morceaux[i].Substring(0, deux), CultureInfo.InvariantCulture) / 1000;
                cles[i] = L(morceaux[i].Substring(deux + 1));
            }
            return ClesP(famille, nom, temps, cles, objet, vitesse);
        }

        static Anim ClesP(string famille, string nom, double[] temps, double[][] cles, string objet = null, double vitesse = 0)
        {
            double total = temps[temps.Length - 1];
            Anim x = Ajouter(famille, nom, total, 1, t => Courbe(temps, cles, t * total));
            x.Objet = objet;
            x.Vitesse = vitesse;
            return x;
        }

        static double[] Courbe(double[] temps, double[][] cles, double t)
        {
            int n = cles.Length, i = 0;
            while (i < n - 2 && t > temps[i + 1]) i++;
            double u = Math.Max(0, Math.Min(1, (t - temps[i]) / (temps[i + 1] - temps[i])));
            double[] p0 = cles[Math.Max(i - 1, 0)], p1 = cles[i], p2 = cles[i + 1], p3 = cles[Math.Min(i + 2, n - 1)];
            var p = new double[I.N];
            for (int k = 0; k < I.N; k++)
                p[k] = 0.5 * (2 * p1[k] + (p2[k] - p0[k]) * u + (2 * p0[k] - 5 * p1[k] + 4 * p2[k] - p3[k]) * u * u
                              + (3 * p1[k] - p0[k] - 3 * p2[k] + p3[k]) * u * u * u);
            p[I.Ge1] = Math.Min(p[I.Ge1], 0);
            p[I.Ge2] = Math.Min(p[I.Ge2], 0);
            return p;
        }

        // La même animation de l'autre main (et de l'autre jambe).
        static Anim Autre(Anim a, string suffixe = " (autre main)")
        {
            Anim b = Ajouter(a.Famille, a.Nom + suffixe, a.Duree, a.Tours, t => Echange(a.Pose(t)));
            b.Haut = a.Haut; b.Objet = a.Objet; b.Vitesse = a.Vitesse; b.Deplace = a.Deplace;
            return b;
        }

        // Une main qui descend sous la ligne du sol (par-dessus un rebord) : ce sont les pieds qui restent posés.
        static Anim PiedsPoses(Anim a)
        {
            Func<double, double[]> pose = a.Pose;
            a.Pose = t =>
            {
                double[] p = pose(t);
                p[I.Air] = 0;
                Dessin.Os o = Dessin.Calculer(p, false);
                p[I.Air] = Math.Max(Math.Max(o.P1.Y, o.P2.Y), Math.Max(o.G1.Y, o.G2.Y));
                return p;
            };
            return a;
        }

        public static void Construire()
        {
            SautMonte = L("-4 -10 150 20 -160 -10 30 -50 -10 -30");
            SautDescend = L("6 5 120 40 -120 -40 20 -30 -15 -40");
            Reception = Cles(null, "Réception", "0:" + Cr + ";180:" + Cr + ";420:" + S);
            SeReleve = Cles(null, "Se relève", "0:" + Ventre + ";500:" + Ventre + ";800:" + Planche + ";1050:" + Cr + ";1350:" + S);
            Groupe = L(Boule);
            const string troisPoints = "32 -22 20 0 -105 -15 70 -120 -10 -100";
            Heros = Cles(null, "Atterrissage de héros", "0:" + troisPoints + ";650:" + troisPoints + ";1000:" + S);
            Concentration = Cles(null, "Disparaît", "0:" + S + ";250:" + Cr + ";450:" + Cr);
            Apparition = Cles(null, "Apparaît", "0:" + Cr + ";150:" + Cr + ";450:" + S);

            // mode farceur : accroupi au bord d'une fenêtre, il lève la main puis l'enfonce sur la croix, sous ses pieds
            const string vise = "70 12 62 78 -50 20 100 -150 88 -146", presse = "78 6 4 0 -50 20 100 -150 88 -146";
            Appuie = PiedsPoses(Cles(null, "Appuie sur la croix", "0:" + S + ";320:" + vise + ";760:" + vise + ";880:" + presse));
            Relache = PiedsPoses(Cles(null, "Relâche la croix", "0:" + presse + ";450:" + presse + ";650:" + vise + ";1000:" + S));

            // correcteur d'orthographe : en l'air devant le mot, il arme le bras puis le pointe du crayon
            const string pointe = "10 4 92 0 -10 12 8 0 -8 0";
            Clique = Cles(null, "Pointe le mot", "0:" + S + ";200:4 0 60 100 -10 12 6 0 -6 0;380:" + pointe + ";560:" + pointe, "crayon");
            Pointe = Fixe(null, "Corrige", pointe, 3, "crayon");

            // bâton de commande : il le lève, le tient pendant que la commande s'écrit ; et ce que font les commandes
            const string leve = "-4 -10 150 10 -10 12 6 0 -6 0";
            LeveBaton = Cles(null, "Lève son bâton", "0:" + S + ";250:-2 -4 70 60 -10 12 6 0 -6 0;500:" + leve, "commande");
            TientBaton = Cles(null, "Tient son bâton levé", "0:" + leve + ";550:-6 -14 158 4 -14 14 8 -4 -8 -4;1100:" + leve, "commande");
            Appui = Fixe(null, "S'appuie sur son bâton", "6 4 40 30 -10 12 10 -6 -6 -2", 3, "commande");
            Tournoie = Ajouter(null, "Fait tournoyer son bâton", 0.5, 6, t => { double[] p = L(S); p[I.Ep1] = 360 * t; p[I.Co1] = 10; return p; });
            Tournoie.Objet = "commande";
            Sursaut = Cles(null, "Sursaute", "0:" + S + ";120:-20 -20 120 40 -120 -40 20 -30 -20 -30 14 0 -10;400:-14 -10 100 60 -100 -60 14 -20 -14 -20 0 0 -16;900:" + S);
            Levitation = Cles(null, "Lévite", "0:" + S + ";600:-6 -10 60 20 -60 -20 10 -20 -10 -30 60;1500:-10 -14 80 10 -80 -10 14 -30 -6 -40 120;2600:-8 -12 70 20 -70 -20 8 -24 -12 -34 116;3200:6 5 120 40 -120 -40 20 -30 -15 -40 30;3500:" + Cr + ";3800:" + S);
            Creatif = Cles(null, "Vole en mode créatif", "0:" + S + ";300:" + Cr + ";700:0 -10 20 10 -20 10 6 -10 -6 -14 90;1500:10 -20 60 0 -40 10 10 -20 -10 -30 120 20;2400:-10 -20 -40 10 60 0 10 -30 -10 -20 100 -20;3200:0 -10 20 10 -20 10 6 -10 -6 -14 80;3700:" + Cr + ";4000:" + S);
            Rugit = Cles(null, "Rugit", "0:" + S + ";250:-14 -30 150 30 -150 -30 20 -20 -20 -20;700:-18 -36 158 20 -158 -20 24 -26 -24 -26;1100:10 10 60 60 -60 -60 16 -16 -16 -16;1500:" + S);
            AssisFeu = Osc(null, "Assis devant le feu", "8 4 55 50 50 55 86 -6 82 -2", "10 6 60 44 54 50 86 -6 82 -2", 1.6, 5);

            Attentes();
            Deplacements();
            Danses();
            Gestes();
            Combat();
            Acrobaties();
            Sport();
            Quotidien();
            Emotions();
            Nouveautes();
            Troisieme();
            Minecraft();
            Decors();
            Quatrieme();
            Cinquieme();
            Sixieme();
            Septieme();
            Huitieme();
            Neuvieme();
            Dixieme();
            Onzieme();
            Douzieme();
            Sonoriser();
            ADeux();
        }

        // ------------------------------------------------------------ Minecraft
        // Deux scènes entières (le moteur s'en charge : voir Speciale) et une animation simple.

        static void Minecraft()
        {
            const string F = "Minecraft";
            CultureInfo c = CultureInfo.InvariantCulture;
            string b = " " + Bloc.ToString(c), haut1 = " " + (Bloc + 14).ToString(c), haut2 = " " + (Bloc + 10).ToString(c);
            const string monte = "-4 -10 150 20 20 30 30 -50 -10 -30", pose = "12 22 35 20 25 30 45 -75 25 -65";

            // --- les morceaux dont les scènes se servent
            // un saut sur place pendant lequel un bloc apparaît sous ses pieds : il retombe un bloc plus haut
            PoseBloc = Cles(null, "Pose un bloc", "0:" + S + ";120:" + Cr + ";300:" + monte + haut1 + ";430:" + pose + haut2 + ";560:" + Cr + b + ";700:" + S + b);
            // le même en avançant d'un bloc : les marches d'un escalier
            PoseMarche = Cles(null, "Pose une marche", "0:" + S + ";120:" + Cr + ";300:" + monte + haut1 + " 0 " + (Bloc * 0.45).ToString(c)
                + ";430:" + pose + haut2 + " 0 " + (Bloc * 0.9).ToString(c) + ";560:" + Cr + b + " 0" + b + ";700:" + S + b + " 0" + b);
            foreach (Anim a in new[] { PoseBloc, PoseMarche }) { a.Bruits = new[] { "pop" }; a.BruitsA = new[] { 0.55 }; }
            Admire = Cles(null, "Admire la vue", "0:" + S + ";250:-6 -10 25 100 -25 -100 8 0 -8 0;900:-6 12 25 100 -25 -100 8 0 -8 0;1500:-6 -10 25 100 -25 -100 8 0 -8 0;1750:" + S);
            Elan = Cles(null, "Élan", "0:" + S + ";200:" + Cr + ";380:" + Cr);
            PoseElytres = L("0 -25 -15 5 -25 5 4 0 -4 0");
            Lance = Cles(null, "Lance", "0:" + S + ";180:-10 -5 -150 -20 30 20 10 -8 -10 -4;330:14 5 100 5 -20 30 18 -14 -14 -4;520:" + S);
            PoseDevant = Cles(null, "Pose devant lui", "0:" + S + ";220:35 20 60 10 30 30 40 -70 20 -50;420:35 20 62 12 30 30 40 -70 20 -50;600:" + S);
            Oreilles = Cles(null, "Accroupi, les mains sur les oreilles", "0:25 25 150 130 -150 -130 55 -100 45 -95;400:27 28 152 132 -152 -132 57 -102 47 -97;800:25 25 150 130 -150 -130 55 -100 45 -95");
            Ramasse = Cles(null, "Ramasse l'eau", "0:" + Cr + ";250:45 25 50 10 20 30 62 -112 52 -104;450:" + Cr + ";700:" + S, "seau");

            // --- les scènes (le moteur fait le reste : décor, déplacements, chutes)
            Scene(F, "Tour de blocs", "tour", PoseBloc.Pose, 0.7, null);
            Scene(F, "Escalier de blocs", "escalier", PoseMarche.Pose, 0.7, null);
            double[] vol = L("0 -25 -15 5 -25 5 4 0 -4 0 46 90");
            Scene(F, "Vol en élytres", "elytres", t => { var p = (double[])vol.Clone(); p[I.Rot] = 90 - 25 * Math.Cos(2 * Math.PI * t); return p; }, 2, "elytres");
            Scene(F, "Seau d'eau au dernier moment", "eau", Ramasse.Pose, 0.7, "seau");
            Scene(F, "Perle de l'Ender", "perle", Lance.Pose, 0.52, null);
            Scene(F, "TNT", "tnt", Oreilles.Pose, 0.8, null);
            Scene(F, "Feu d'artifice", "fusee", PoseDevant.Pose, 0.6, null);
            Scene(F, "Portail du Nether", "portail", Concentration.Pose, 0.45, null);

            // --- les animations simples, avec les objets du jeu
            Bruit(Osc(F, "Mine avec une pioche", "-6 -5 150 40 -10 12 12 -8 -10 -4", "14 6 45 5 -10 12 16 -12 -12 -4", 0.5, 8, "pioche"), "coup", 0.45);
            Bruit(Osc(F, "Tape un arbre à mains nues", "4 0 40 100 60 90 12 -8 -10 -4", "10 0 85 5 40 110 16 -12 -12 -4", 0.36, 10, "arbre"), "coup", 0.4);
            const string forge1 = "12 8 70 40 60 50 12 -8 -10 -4", forge2 = "12 10 55 30 75 60 12 -8 -10 -4";
            Cles(F, "Fabrique une épée sur l'établi", "0:" + S + ";300:" + forge1 + ";500:" + forge2 + ";700:" + forge1 + ";900:" + forge2 + ";1100:" + forge1
                + ";1500:0 -10 100 20 -10 12 8 0 -8 0;2400:-4 -14 150 10 -10 12 8 0 -8 0;2800:" + S, "craft");
            Fixe(F, "Dort dans un lit", "0 10 25 5 28 -5 15 0 12 0 18 -75", 4, "lit");
            Osc(F, "Croque une pomme dorée", "0 8 60 125 -8 12", "0 14 70 140 -8 12", 0.5, 6, "pomme");
            Osc(F, "Boit une potion", "-6 -18 70 135 -8 12", "-10 -28 78 142 -8 12", 0.9, 3, "potion");
            Bruit(Cles(F, "Combo à l'épée en diamant", "0:" + G + ";150:-10 -5 160 30 55 110 14 -15 -14 -10;300:22 5 40 0 50 115 24 -22 -22 -6 0 0 8;450:" + G
                + ";600:8 0 -120 -20 55 110 14 -15 -14 -10;780:16 0 95 0 50 115 22 -20 -20 -6 0 0 6;950:" + G
                + ";1100:-12 -8 170 10 60 100 30 -50 -10 -30 20;1300:26 8 50 0 50 115 40 -70 20 -60;1550:" + G, "epeediamant"), "swish", 0.15);
            // il rebondit de plus en plus haut sur un bloc de slime, et finit par un salto
            Cles(F, "Trampoline de slime", "0:" + S + b + ";150:" + Cr + b + ";400:" + monte + " 78;650:" + Cr + b + ";900:" + monte + " 110;1150:" + Cr + b
                + ";1400:" + Boule + " 150 180;1650:" + monte + " 120 360;1900:" + Cr + b + " 360;2150:" + S + b + " 360", "slime");

            // --- en voiture : assis dans un wagonnet ou un bateau, qui cachent ses jambes
            double[] assis = L("-4 0 55 30 45 40 80 -85 74 -80 14");
            Anim wagon = Ajouter(F, "Roule en wagonnet", 0.5, 1, t =>
            {
                var p = (double[])assis.Clone();
                p[I.Air] = 14 + 1.5 * Math.Sin(4 * Math.PI * t);
                p[I.Ep1] = 150 + 12 * Math.Sin(2 * Math.PI * t); p[I.Co1] = 15;
                return p;
            });
            wagon.Deplace = true; wagon.Vitesse = 320; wagon.Objet = "wagon";
            Anim bateau = Ajouter(F, "Rame en bateau", 1.0, 1, t =>
            {
                double w = Math.Sin(2 * Math.PI * t);
                var p = (double[])assis.Clone();
                p[I.Air] = 12 + 1.5 * w; p[I.Torse] = -4 + 14 * w;
                p[I.Ep1] = 60 + 35 * w; p[I.Co1] = 30 - 20 * w; p[I.Ep2] = 50 + 35 * w; p[I.Co2] = 40 - 20 * w;
                return p;
            });
            bateau.Deplace = true; bateau.Vitesse = 130; bateau.Objet = "bateau";
            Anim torche = Pas("Explore à la torche", 16, 26, 12, 10, 2, 0, 40, 1.3);
            Func<double, double[]> marche = torche.Pose;
            torche.Pose = t => { var p = marche(t); p[I.Ep1] = 78; p[I.Co1] = 22; p[I.Tete] = 8 * Math.Sin(2 * Math.PI * t); return p; };
            torche.Famille = F; torche.Objet = "torche";
        }

        static Anim Scene(string famille, string nom, string special, Func<double, double[]> pose, double duree, string objet)
        {
            Anim a = Ajouter(famille, nom, duree, 1, pose);      // la pose ne sert qu'aux planches de contrôle
            a.Special = special;
            a.Objet = objet;
            return a;
        }

        // ------------------------------------------------------------ commandes, décor, objets du jeu

        static void Decors()
        {
            // --- le bâton de commande : réservé au premier stickman (le moteur vérifie)
            const string K = "Commandes";
            foreach (string[] c in new[]
            {
                new[] { "/tp : se téléporte", "tp" }, new[] { "/tp @a : appelle toute la bande", "tpa" }, new[] { "/setblock : pose un bloc", "setblock" },
                new[] { "/give : une épée en diamant", "give" }, new[] { "/summon : la foudre", "foudre" }, new[] { "/summon : un feu d'artifice", "feu" },
                new[] { "/summon : une TNT", "tnt" }, new[] { "/effect : lévitation", "levitation" }, new[] { "/effect : vitesse", "vitesse" },
                new[] { "/weather : la pluie", "pluie" }, new[] { "/time : la nuit", "nuit" }, new[] { "/gamemode : vole en créatif", "creatif" },
                new[] { "/particle : des cœurs", "coeurs" }, new[] { "/say : salue tout le monde", "dire" }, new[] { "/fill : fait apparaître la maison", "maison" },
            })
                Scene(K, c[0], "cmd:" + c[1], TientBaton.Pose, 1.1, "commande");
            Scene(K, "S'appuie sur son bâton", "cmd:appui", Appui.Pose, 1.6, "commande");
            Scene(K, "Fait tournoyer son bâton", "cmd:tournoie", Tournoie.Pose, 0.5, "commande");

            // --- le décor : la maison, le feu de camp, le jardin
            const string D = "Décor";
            Scene(D, "Rentre à la maison", "maison", Concentration.Pose, 0.45, null);
            Scene(D, "Fait la sieste à la maison", "sieste", Concentration.Pose, 0.45, null);
            Scene(D, "Bâtit la maison de ses mains", "batir", PoseDevant.Pose, 0.6, null);
            Scene(D, "Feu de camp", "camp", AssisFeu.Pose, 1.6, null);
            Scene(D, "Plante des fleurs", "jardin", PoseDevant.Pose, 0.6, null);
            Scene(D, "Fait griller un poulet", "grillade", AssisFeu.Pose, 1.6, null);
            Scene(D, "Bâtit un golem de neige", "golem", PoseDevant.Pose, 0.6, null);
            Scene(D, "Fait pousser un arbre", "arbre", PoseDevant.Pose, 0.6, null);

            // --- les tours spéciaux, chacun avec sa case dans l'onglet « Spécial » des paramètres
            const string X = "Spécial";
            Scene(X, "Devient géant", "x:geant", Rugit.Pose, 1.5, null);
            Scene(X, "Devient minuscule", "x:mini", Sursaut.Pose, 0.9, null);
            Scene(X, "Dessine sur l'écran", "x:dessin", Pointe.Pose, 1.6, "crayon");
            Scene(X, "Ouvre YouTube sur la chaîne d'Alan Becker", "x:youtube", Admire.Pose, 1.75, null);
            Scene(X, "Attrape le curseur au lasso", "x:curseur", Lance.Pose, 0.52, null);
            Scene(X, "Secoue sa fenêtre", "x:secousse", Admire.Pose, 1.75, null);

            // --- objets du jeu tenus en main (« item: » tel quel, « outil: » aligné sur l'avant-bras)
            const string F = "Minecraft";
            const string mange1 = "0 8 60 125 -8 12", mange2 = "0 14 70 140 -8 12";
            Osc(F, "Mange un steak", mange1, mange2, 0.5, 6, "item:cooked_beef");
            Osc(F, "Grignote un cookie", mange1, mange2, 0.35, 8, "item:cookie");
            Osc(F, "Mange du pain", mange1, mange2, 0.55, 5, "item:bread");
            Osc(F, "Croque une carotte dorée", mange1, mange2, 0.45, 6, "item:golden_carrot");
            Osc(F, "Boit un seau de lait", "-6 -18 70 135 -8 12", "-10 -28 78 142 -8 12", 0.9, 3, "item:milk_bucket");
            Osc(F, "Regarde sa boussole", "6 18 60 105 -8 12", "6 22 62 110 -8 12", 1.2, 3, "item:compass_16");
            Osc(F, "Regarde l'heure", "6 18 60 105 -8 12", "4 14 64 100 -8 12", 1.0, 3, "item:clock_00");
            Osc(F, "Observe à la longue-vue", "4 -10 100 120 -8 12", "6 -6 102 124 -8 12", 1.4, 3, "item:spyglass");
            Osc(F, "Lit une carte", "8 20 55 95 50 100", "8 24 57 98 52 97", 1.3, 3, "item:filled_map");
            Osc(F, "Lit un livre enchanté", "8 20 55 95 50 100", "8 24 57 98 52 97", 1.5, 3, "item:enchanted_book");
            Osc(F, "Brandit un totem", "-4 -12 160 10 -8 12", "-6 -16 166 4 -8 12", 0.6, 5, "item:totem_of_undying");
            Osc(F, "Admire un diamant", "0 -8 110 60 -8 12", "0 -12 118 66 -8 12", 0.8, 4, "item:diamond");
            Osc(F, "Compte ses émeraudes", "6 16 60 110 50 120", "6 18 62 114 54 112", 0.5, 6, "item:emerald");
            Osc(F, "Sonne la cloche", "0 -4 100 40 -8 12", "0 -4 100 80 -8 12", 0.4, 6, "item:bell").Son = "note";
            Osc(F, "Souffle dans une corne", "-4 -10 105 150 -8 12", "-6 -14 107 152 -8 12", 1.0, 3, "item:goat_horn").Son = "note";
            Cles(F, "Lance une boule de neige", "0:" + S + ";180:-10 -5 -150 -20 30 20 10 -8 -10 -4;330:14 5 100 5 -20 30 18 -14 -14 -4;520:" + S, "item:snowball");
            Bruit(Osc(F, "Coupe du bois à la hache", "-6 -5 150 40 -10 12 12 -8 -10 -4", "14 6 45 5 -10 12 16 -12 -12 -4", 0.6, 7, "outil:diamond_axe"), "coup", 0.45);
            Bruit(Osc(F, "Creuse à la pelle", "20 14 70 30 40 60 20 -24 -14 -10", "34 22 40 20 20 50 28 -36 -14 -16", 0.7, 6, "outil:diamond_shovel"), "coup", 0.45);
            Osc(F, "Bêche son jardin", "16 10 110 20 60 60 16 -16 -14 -8", "30 18 50 10 30 50 24 -28 -14 -12", 0.8, 6, "outil:diamond_hoe");
            Bruit(Cles(F, "Lance le trident", "0:" + G + ";220:-12 -6 -160 -10 60 100 14 -15 -14 -10;380:18 4 100 0 50 115 24 -22 -22 -6 0 0 8;620:" + G, "outil:trident"), "swish", 0.5);
            Anim lanterne = Pas("Avance à la lanterne", 16, 26, 12, 10, 2, 0, 40, 1.3);
            Func<double, double[]> marche = lanterne.Pose;
            lanterne.Pose = t => { double[] p = marche(t); p[I.Ep1] = 70; p[I.Co1] = 20; return p; };
            lanterne.Famille = F; lanterne.Objet = "item:lantern";
        }

        // ------------------------------------------------------------ septième fournée

        static void Septieme()
        {
            // --- déplacements
            Pas("Marche de mannequin", 30, 36, 10, 10, -6, 0, 75, 0.85);
            Pas("Démarche de cow-boy", 20, 30, 6, 40, -8, 0, 50, 1.1);
            Pas("Pas de géant, lents", 50, 40, 30, 10, 4, 0, 90, 1.4);
            Pas("Trotte en sifflotant", 26, 46, 30, 20, -4, 3, 95, 0.7);
            Pas("Marche sur des œufs", 10, 30, 8, 40, 6, 0, 28, 1.0);
            Pas("Fonce tête baissée", 48, 96, 20, 100, 34, 5, 280, 0.38);
            Pas("Marche au pas cadencé", 36, 6, 40, 0, -2, 0, 85, 0.7);
            Pas("Flâne les mains dans le dos", 18, 28, 0, 0, -6, 0, 38, 1.3);

            // --- gestes
            Osc("Gestes", "Soulève un chapeau invisible", "0 6 150 130 -8 12", "4 14 120 110 -8 12", 0.9, 3);
            Osc("Gestes", "Mime une vitre", "0 0 100 30 -100 -30", "0 0 80 50 -80 -50", 0.7, 5);
            Osc("Gestes", "Tape des mains au-dessus de la tête", "0 -8 165 30 -165 -30", "0 -8 172 4 -172 -4", 0.35, 10);
            Osc("Gestes", "Se frotte le ventre", "-4 0 30 110 -8 12", "-4 0 40 100 -8 12", 0.5, 6);
            Osc("Gestes", "Tend l'oreille", "10 -14 130 150 -8 12", "14 -18 132 152 -8 12", 1.2, 3);
            Osc("Gestes", "Envoie un bisou des deux mains", "0 0 100 150 -100 -150", "0 -8 110 40 -110 -40", 0.8, 4, "coeur");
            Osc("Gestes", "Salue comme une reine", "0 0 120 110 -8 12", "0 0 126 100 -8 12", 0.5, 6);
            Osc("Gestes", "Mouline d'un seul bras", "0 0 0 10 -8 12", "0 0 180 10 -8 12", 0.5, 6);
            Osc("Gestes", "Fait le signe de la paix", "0 -4 140 30 -8 12", "0 -6 146 36 -8 12", 0.7, 4);
            Osc("Gestes", "Fait craquer ses doigts", "6 6 70 60 62 68", "6 8 62 76 54 84", 0.6, 4);
            Osc("Gestes", "Lance une pièce", "0 -10 95 70 -8 12", "0 -16 120 30 -8 12", 0.7, 4);
            Osc("Gestes", "Porte un toast", "-2 -6 110 30 -8 12", "-4 -10 130 20 -8 12", 0.9, 3);

            // --- combat
            Cles("Combat", "Coup de pied retourné", "0:" + G + ";200:-10 0 60 100 50 110 20 -30 -10 -10 0 60;380:-20 -10 80 60 60 100 -100 -5 10 -20 6 170;560:-10 0 70 90 55 105 -40 -30 12 -16 0 300;760:" + G + " 0 360");
            Cles("Combat", "Direct en reculant", "0:" + G + ";160:14 0 92 0 50 115 20 -20 -24 -8 0 0 -6;320:" + G + " 0 0 -12;520:" + G);
            Cles("Combat", "Coup de marteau des deux poings", "0:" + G + ";220:-12 -10 170 20 165 25 12 -10 -12 -8 6;400:30 14 50 10 46 14 30 -44 -14 -20;620:30 14 50 10 46 14 30 -44 -14 -20;850:" + G);
            Osc("Combat", "Garde haute, pas chassés", "6 0 90 120 80 125 20 -24 -18 -14 0 0 8", "6 0 90 120 80 125 14 -16 -24 -22 0 0 -8", 0.5, 8);
            Cles("Combat", "Esquive en pivotant", "0:" + G + ";180:-30 -14 60 90 50 100 30 -40 -20 -10 0 0 -10;380:20 10 60 90 50 100 10 -10 -30 -40 0 0 6;560:" + G);
            Cles("Combat", "Prise et projection", "0:" + G + ";250:30 10 80 20 70 30 30 -40 -14 -14;500:-20 -10 150 60 140 70 20 -30 -10 -10;700:50 20 30 20 20 30 50 -80 -10 -20;1000:" + G);
            Osc("Combat", "Rafale de directs", "14 0 92 0 55 115 20 -20 -20 -8 0 0 4", "14 0 55 115 92 0 20 -20 -20 -8 0 0 4", 0.16, 18);

            // --- acrobaties
            Cles("Acrobaties", "Roue sur un bras", "0:" + S + ";250:60 0 150 0 -40 20 60 -10 -30 0 0 60;500:0 0 180 0 -60 10 50 0 -50 0 0 180;750:-40 0 150 0 -40 20 30 0 -60 -10 0 300;1000:" + S + " 0 360");
            Cles("Acrobaties", "Grand écart sauté", "0:" + S + ";200:" + Cr + ";450:0 -10 150 0 -150 0 88 0 -88 0 58;650:0 -10 150 0 -150 0 80 0 -80 0 44;850:" + Cr + ";1050:" + S);
            Cles("Acrobaties", "Saut de chat", "0:" + S + ";180:" + Cr + ";360:-4 -8 60 20 -60 -20 80 -100 -10 -20 34;520:-4 -8 -60 -20 60 20 -10 -20 80 -100 40;700:" + Cr + ";880:" + S);
            Osc("Acrobaties", "Pompes en équilibre", "0 0 180 0 176 0 4 0 -4 0 0 180", "0 0 150 60 146 64 4 0 -4 0 0 180", 1.2, 4);
            Cles("Acrobaties", "Salto vrillé", "0:" + S + ";180:" + Cr + ";380:" + Boule + " 62 150;560:0 0 170 0 -170 0 4 0 -4 0 70 280;740:" + Cr + " 8 360;940:" + S + " 0 360");
            Cles("Acrobaties", "Chandelle", "0:" + S + ";400:" + Dos + ";800:0 0 -60 -30 -64 -30 10 0 6 0 0 -170;1600:0 0 -60 -30 -64 -30 6 0 10 0 0 -176;2000:" + Dos + ";2500:" + Cr + ";2800:" + S);

            // --- sport
            Osc("Sport", "Dribble du pied", "6 8 20 30 -30 20 40 -50 -10 -10", "6 8 -30 20 20 30 -10 -10 40 -50", 0.4, 10, "ballonpied");
            Cles("Sport", "Lancer de poids", "0:" + S + ";350:-20 -10 70 150 -60 -20 20 -40 -20 -10;600:24 4 130 10 -30 20 30 -20 -30 -10 0 0 8;900:" + S);
            Cles("Sport", "Saut en longueur sur place", "0:" + S + ";300:30 10 -60 -10 -70 -10 60 -100 50 -92;520:-10 -10 150 10 140 15 60 -20 50 -16 40;720:20 10 80 10 70 15 90 -60 80 -56 20;900:" + Cr + ";1150:" + S);
            Osc("Sport", "Crawl à sec", "60 30 170 0 20 0 14 0 -6 0", "60 30 20 0 170 0 -6 0 14 0", 0.7, 8);
            Osc("Sport", "Brasse à sec", "50 24 160 0 156 0 10 -10 -10 -10", "50 24 60 100 56 104 40 -70 30 -64", 1.0, 6);
            Osc("Sport", "Grimpe à un mur invisible", "0 -16 170 20 60 90 80 -100 0 -5 10", "0 -16 60 90 170 20 0 -5 80 -100 10", 0.7, 8);
            Cles("Sport", "Penalty", "0:" + S + ";250:-14 0 60 20 -80 -20 -50 -60 10 -10;420:10 0 -40 -10 90 10 80 -5 -10 -12;640:6 0 -20 0 60 10 40 -20 -8 -8;900:" + S, "ballontir");
            Osc("Sport", "Moulinets de hanches", "8 0 20 100 -20 -100 14 -6 -2 0 0 0 6", "-8 0 20 100 -20 -100 2 0 -14 -6 0 0 -6", 0.9, 6);

            // --- quotidien
            Osc("Quotidien", "Cherche ses clés dans ses poches", "6 14 10 60 -10 12 6 0 -6 0", "6 16 -10 12 14 64 6 0 -6 0", 0.7, 5, "question");
            Osc("Quotidien", "Tape un texto", "6 22 55 115 50 120 6 0 -6 0", "6 24 57 112 52 117 6 0 -6 0", 0.18, 16, "telephone");
            Osc("Quotidien", "Peint un mur au rouleau", "0 -8 160 10 -10 12 8 0 -8 0", "0 0 100 10 -10 12 8 -6 -8 -4", 0.9, 5);
            Osc("Quotidien", "Visse une ampoule", "-4 -24 170 30 -10 12 6 0 -6 0", "-4 -24 170 60 -10 12 6 0 -6 0", 0.35, 8);
            Osc("Quotidien", "Essuie une vitre", "0 -4 130 30 -10 12 8 0 -8 0", "0 -4 100 60 -10 12 8 0 -8 0", 0.4, 8);
            Osc("Quotidien", "Noue sa cravate", "4 20 50 130 46 134 6 0 -6 0", "4 22 54 126 42 138 6 0 -6 0", 0.5, 6);
            Osc("Quotidien", "Porte un plateau", "-4 0 80 90 -10 12 8 -4 -8 -2", "-4 2 82 88 -10 12 10 -8 -6 0", 1.0, 4);
            Cles("Quotidien", "Ramasse quelque chose par terre", "0:" + S + ";350:70 30 30 0 -20 20 30 -30 -10 -10;650:70 30 24 0 -20 20 30 -30 -10 -10;1000:0 10 60 110 -10 12 6 0 -6 0;1500:" + S);

            // --- émotions
            Osc("Émotions", "Bombe le torse", "-12 -14 -30 -100 -35 -105 8 0 -8 0", "-16 -18 -36 -96 -41 -101 8 0 -8 0", 1.2, 3);
            Osc("Émotions", "Sautille d'excitation", "0 -6 60 100 -60 -100 8 -12 -8 -12 10", "0 -8 70 110 -70 -110 6 0 -6 0", 0.26, 12);
            Cles("Émotions", "S'incline, vaincu", "0:" + S + ";500:30 30 10 10 -10 10 40 -70 30 -64;1500:34 36 10 10 -10 10 44 -76 34 -70;2000:" + S);
            Osc("Émotions", "Panique, court sur place", "10 -10 150 30 -150 -30 60 -80 -10 -40 4", "10 -10 150 30 -150 -30 -10 -40 60 -80 4", 0.18, 16, "exclam");
            Cles("Émotions", "Saute de surprise et retombe assis", "0:" + S + ";140:-10 -20 140 20 -140 -20 20 -30 -20 -30 30;420:-10 0 30 20 -30 20 86 -10 80 -6;1300:-12 0 34 16 -34 16 86 -10 80 -6;1700:" + Cr + ";2000:" + S, "exclam");
            Osc("Émotions", "Jubile en silence", "0 -10 60 130 -60 -130 8 -6 -8 -6", "0 -14 70 140 -70 -140 10 -12 -10 -12", 0.3, 8);
        }

        // ------------------------------------------------------------ huitième fournée

        static void Huitieme()
        {
            // --- déplacements
            Pas("Marche de pingouin", 8, 6, 4, 0, 0, 0, 22, 0.45);
            Pas("Course de dessin animé", 60, 110, 80, 60, 20, 8, 340, 0.26);
            Pas("Trottine, les bras en l'air", 24, 50, 0, 0, -4, 4, 110, 0.5, 3);
            Pas("Robot rouillé", 22, 30, 20, 60, 0, 0, 34, 1.2, 0, 0, 0, true);
            Pas("Promenade du dimanche", 20, 22, 14, 6, -4, 1, 42, 1.25);
            Pas("Marche contre le vent", 22, 30, 0, 0, 30, 0, 20, 1.3, 5);
            Pas("Traîne les pieds, épuisé", 12, 8, 4, 0, 26, 0, 18, 1.6);
            Anim roue = Ajouter("Déplacements", "Avance en faisant la roue", 1.1, 1, t =>
            {
                double[] p = L("0 0 165 0 -165 0 28 0 -28 0");
                p[I.Rot] = 360 * t;
                return p;
            });
            roue.Deplace = true; roue.Vitesse = 280; roue.Son = "swish";
            Anim glisse = Ajouter("Déplacements", "Glisse sur le ventre comme un pingouin", 1.0, 1, t =>
            {
                double w = Math.Sin(2 * Math.PI * t);
                double[] p = L("0 -24 -14 0 -20 0 -8 0 -4 0 0 82");
                p[I.Ep1] = -14 + 10 * w; p[I.Ep2] = -20 - 10 * w; p[I.Rot] = 82 + 2 * w;
                return p;
            });
            glisse.Deplace = true; glisse.Vitesse = 170; glisse.Son = "glisse";
            Anim ski = Ajouter("Déplacements", "Ski de fond", 0.9, 1, t =>
            {
                double w = Math.Sin(2 * Math.PI * t);
                double[] p = L("18 -8 0 10 0 10 0 -12 0 -12");
                p[I.Ep1] = 20 + 55 * w; p[I.Ep2] = 20 - 55 * w; p[I.Ha1] = 4 - 26 * w; p[I.Ha2] = 4 + 26 * w;
                return p;
            });
            ski.Deplace = true; ski.Vitesse = 130; ski.Objet = "planche";
            Anim joints = Ajouter("Déplacements", "Sautille à pieds joints", 0.45, 1, t =>
            {
                double u = Math.Max(0, Math.Sin(2 * Math.PI * t));
                double[] p = L("6 0 -20 20 -24 20 30 -60 26 -56");
                p[I.Air] = 20 * u; p[I.Ha1] = 30 - 22 * u; p[I.Ge1] = -60 + 50 * u; p[I.Ha2] = 26 - 22 * u; p[I.Ge2] = -56 + 50 * u;
                p[I.Ep1] = -20 + 60 * u; p[I.Ep2] = -24 + 60 * u;
                return p;
            });
            joints.Deplace = true; joints.Vitesse = 80;
            Anim galop = Ajouter("Déplacements", "Galope sur un cheval invisible", 0.4, 1, t =>
            {
                double f = 2 * Math.PI * t;
                double[] p = L("10 -6 62 40 58 44 0 0 0 0");
                p[I.Ha1] = 28 * Math.Sin(f); p[I.Ge1] = -30 - 30 * Math.Max(0, Math.Cos(f));
                p[I.Ha2] = 28 * Math.Sin(f - 0.9); p[I.Ge2] = -30 - 30 * Math.Max(0, Math.Cos(f - 0.9));
                p[I.Air] = 9 * Math.Abs(Math.Sin(f)); p[I.Torse] = 10 + 4 * Math.Sin(f);
                return p;
            });
            galop.Deplace = true; galop.Vitesse = 210;
            Anim crabe = Ajouter("Déplacements", "Marche du crabe", 0.8, 1, t =>
            {
                double w = Math.Sin(2 * Math.PI * t);
                double[] p = L("-60 34 -10 0 -18 0 100 -100 92 -96");
                p[I.Ep1] = -10 + 12 * w; p[I.Ep2] = -18 - 12 * w; p[I.Ha1] = 100 - 10 * w; p[I.Ha2] = 92 + 10 * w;
                return p;
            });
            crabe.Deplace = true; crabe.Vitesse = 45;

            // --- gestes
            Osc("Gestes", "Chasse une mouche", "0 -6 150 20 -8 12", "0 0 70 40 -8 12", 0.25, 8);
            Osc("Gestes", "Se bouche le nez", "0 6 100 150 70 20", "0 8 102 148 100 40", 0.5, 5);
            Osc("Gestes", "Salue derrière lui", "-4 -6 -150 -20 -8 12", "-4 -6 -150 20 -8 12", 0.35, 6);
            Anim moulins = Ajouter("Gestes", "Moulinets inversés", 0.8, 5, t => { double[] p = L(S); p[I.Ep1] = 360 * t; p[I.Co1] = 6; p[I.Ep2] = -360 * t; p[I.Co2] = -6; return p; });
            moulins.Haut = true;
            Osc("Gestes", "Pistolets des doigts", "-4 -4 95 10 85 14", "-8 -8 105 30 95 34", 0.4, 6);
            Cles("Gestes", "Lance un avion en papier", "0:" + S + ";300:-8 -6 -150 -20 -10 12 8 -4 -8 -4;480:10 0 110 0 -10 12 12 -8 -10 -4;1100:6 -8 110 10 -10 12 8 -4 -8 -4;1400:" + S);
            Osc("Gestes", "Se dépoussière l'épaule", "0 6 150 150 -8 12", "0 6 140 130 -8 12", 0.3, 6);
            Osc("Gestes", "Joue de la flûte", "0 4 80 130 60 120", "0 6 82 126 62 124", 0.4, 8, "note");
            Osc("Gestes", "Joue du violon", "-4 10 70 60 110 140", "-4 12 50 100 110 140", 0.5, 8, "note");
            Osc("Gestes", "Joue de la trompette", "-8 -10 100 140 96 144", "-10 -14 102 138 98 142", 0.3, 8, "note");
            Osc("Gestes", "Joue de l'accordéon", "0 0 60 80 40 100", "0 0 80 50 20 130", 0.8, 6, "note");
            Osc("Gestes", "Fait de grands signes", "0 -6 150 -20 -150 20", "0 -6 170 30 -170 -30", 0.35, 10);
            Osc("Gestes", "Se bouche les oreilles", "4 10 150 125 140 135", "6 14 152 127 142 137", 0.8, 4);
            Osc("Gestes", "Motus et bouche cousue", "0 4 96 150 -8 12", "0 4 100 120 -8 12", 0.7, 3);
            Osc("Gestes", "Se tapote la tempe", "0 6 130 150 -8 12", "0 6 134 142 -8 12", 0.25, 8);

            // --- combat
            Cles("Combat", "Double coup de pied sauté", "0:" + G + ";200:" + Cr + ";380:-10 0 60 90 50 100 95 -5 20 -90 30;520:-10 0 60 90 50 100 20 -90 95 -5 34;700:" + Cr + ";900:" + G);
            Cles("Combat", "Coup de genou sauté", "0:" + G + ";180:" + Cr + ";340:-6 0 80 100 60 110 100 -120 -10 -20 26;520:" + Cr + ";700:" + G);
            Cles("Combat", "Blocage et contre", "0:" + G + ";200:-6 -4 120 70 60 110 10 -12 -18 -12 0 0 -6;420:-6 -4 120 70 60 110 10 -12 -18 -12 0 0 -6;560:16 0 55 110 92 0 22 -20 -22 -8 0 0 8;760:" + G);
            const string grue = "0 -6 150 -30 -150 30 80 -100 0 0";
            Cles("Combat", "Coup de pied de la grue", "0:" + S + ";400:" + grue + ";1200:" + grue + ";1380:-10 0 150 -30 -150 30 95 -5 0 -6 24;1560:" + grue + ";2000:" + S);
            Cles("Combat", "Coup de pied marteau", "0:" + G + ";220:-14 0 60 100 50 110 150 -5 -12 -14;380:-6 0 60 100 50 110 150 0 -14 -10;520:14 0 70 90 55 105 40 -10 -14 -12;720:" + G);
            Cles("Combat", "Frappe de la paume", "0:" + G + ";180:6 0 30 120 55 110 14 -15 -14 -10 0 0 -4;330:20 2 95 0 40 100 30 -30 -26 -4 0 0 14;560:" + G);
            Cles("Combat", "Salto arrière frappé", "0:" + G + ";180:" + Cr + ";360:-30 -10 100 20 -100 -20 110 -5 20 -60 30 -90;520:" + Boule + " 44 -220;700:" + Cr + " 0 -360;900:" + G + " 0 -360");
            Cles("Combat", "Esquive en se penchant en arrière", "0:" + G + ";250:-44 -14 -40 -20 -50 -20 30 -80 20 -76;600:-48 -16 -42 -20 -52 -20 31 -82 21 -78;900:-46 -14 -44 -20 -54 -20 30 -80 20 -76;1200:" + G);
            Cles("Combat", "Coup de pied bas puis haut", "0:" + G + ";180:-6 0 70 100 55 110 60 -5 -12 -12;330:" + G + ";500:-16 0 70 100 55 110 125 -5 -14 -16;680:" + G);
            Osc("Combat", "Garde serrée derrière ses gants", "14 10 110 150 100 156 16 -20 -14 -16", "18 14 112 152 102 158 20 -26 -10 -20", 0.4, 6);
            Cles("Combat", "Charge son poing, puis frappe", "0:" + G + ";300:-20 -6 -60 110 55 110 10 -20 -24 -10 0 0 -8;1000:-24 -8 -70 116 55 110 12 -24 -26 -12 0 0 -10;1150:26 4 92 0 40 100 34 -30 -30 -4 0 0 18;1500:26 4 92 0 40 100 34 -30 -30 -4 0 0 18;1800:" + G);

            // --- acrobaties
            Cles("Acrobaties", "Roue puis salto", "0:" + S + ";250:60 0 150 0 -40 20 60 -10 -30 0 0 60;500:0 0 180 0 -180 0 40 0 -40 0 0 180;750:" + Cr + " 0 360;950:" + Boule + " 60 540;1200:" + Cr + " 0 720;1400:" + S + " 0 720");
            Cles("Acrobaties", "Saut de biche", "0:" + S + ";180:" + Cr + ";400:-6 -10 120 10 -120 -10 80 -90 -70 -10 44;600:" + Cr + ";800:" + S);
            const string poirier = "0 0 180 0 176 0 4 0 -4 0 0 180", ecarte = "0 0 180 0 176 0 70 0 -70 0 0 180";
            Cles("Acrobaties", "Poirier, grand écart", "0:" + S + ";350:" + Cr + ";700:" + poirier + ";1100:" + ecarte + ";1700:" + ecarte + ";2000:" + poirier + ";2300:" + Cr + " 0 360;2600:" + S + " 0 360");
            const string fesses = "-10 0 30 20 -30 20 86 -10 80 -6";
            Cles("Acrobaties", "Rebondit sur les fesses", "0:" + S + ";200:" + Cr + ";420:" + fesses + " 30;600:" + fesses + ";760:-10 0 60 20 -60 -20 86 -10 80 -6 22;920:" + fesses + ";1200:" + Cr + ";1450:" + S);
            Cles("Acrobaties", "Salto raté, atterrit sur le dos", "0:" + S + ";200:" + Cr + ";420:" + Boule + " 50 150;600:" + Boule + " 24 240;760:0 10 25 5 28 -5 15 0 12 0 0 285;1700:0 10 25 5 28 -5 15 0 12 0 0 285;2100:" + Cr + " 0 360;2400:" + S + " 0 360", "exclam");
            Cles("Acrobaties", "Deux roues enchaînées", "0:" + S + ";200:60 0 150 0 -40 20 60 -10 -30 0 0 60;450:0 0 170 0 -170 0 50 0 -50 0 0 180;700:-40 0 150 0 -40 20 30 0 -60 -10 0 300;900:" + S + " 0 360;1100:60 0 150 0 -40 20 60 -10 -30 0 0 420;1350:0 0 170 0 -170 0 50 0 -50 0 0 540;1600:-40 0 150 0 -40 20 30 0 -60 -10 0 660;1800:" + S + " 0 720");

            // --- sport
            Osc("Sport", "Manchette de volley", "20 0 60 0 58 0 40 -60 30 -56", "10 -6 84 0 82 0 30 -40 20 -36", 0.6, 6);
            Cles("Sport", "Lancer de fléchettes", "0:" + S + ";350:-4 -4 120 120 -10 12 10 -6 -10 -4;500:6 0 100 10 -10 12 14 -8 -12 -4;800:6 0 100 10 -10 12 14 -8 -12 -4;1100:" + S);
            Cles("Sport", "Revers de tennis", "0:" + S + ";250:-10 0 -40 20 -10 12 16 -16 -14 -10;450:14 0 110 0 -30 20 22 -20 -20 -8;700:" + S, "raquette");
            Osc("Sport", "Putt de golf", "30 20 22 10 -8 12 10 -10 -10 -10", "30 20 44 10 -8 12 10 -10 -10 -10", 1.0, 4, "club");
            Cles("Sport", "Lancer du disque", "0:" + S + ";350:-24 -10 -110 -10 70 60 20 -30 -16 -10;700:-28 -12 -120 -10 74 64 24 -36 -18 -12;900:16 0 120 0 -40 30 24 -18 -24 -8 0 0 8;1300:" + S);
            Cles("Sport", "Plongeon de gardien", "0:" + S + ";200:" + Cr + ";450:20 -10 170 0 160 0 10 0 -10 -10 40 70;700:" + Ventre + ";1300:" + Ventre + ";1600:" + Planche + ";1850:" + Cr + ";2100:" + S);
            Osc("Sport", "Lutte à la corde","-20 0 80 20 70 30 30 -30 -20 -10", "-36 -6 60 90 50 100 40 -50 -24 -6", 0.8, 5);
            Cles("Sport", "Escrime : fente", "0:" + G + ";200:6 0 60 110 -60 -30 10 -20 -20 -10;380:16 0 92 0 -100 -10 60 -50 -40 -4 0 0 16;620:16 0 92 0 -100 -10 60 -50 -40 -4 0 0 16;860:" + G, "epee");
            Cles("Sport", "Smash de badminton", "0:" + S + ";250:-14 -14 -150 -30 60 40 10 -10 -14 -10;420:-10 -12 170 0 40 30 20 -30 -10 -30 30;560:16 6 60 0 -20 20 20 -30 -10 -30 20;760:" + Cr + ";960:" + S, "raquette");
            Osc("Sport", "Rotations du buste", "22 6 -35 105 -45 112 8 0 -8 0", "-20 -6 -35 105 -45 112 8 0 -8 0", 1.0, 5);
            Cles("Sport", "Saut de haies", "0:" + S + ";160:" + Cr + ";340:20 0 100 0 -60 -40 95 -5 -70 -90 30;520:" + Cr + ";700:" + S);
            Cles("Sport", "Pétanque : pointe", "0:" + S + ";500:20 10 -60 0 -10 12 30 -40 -20 -20;780:6 0 100 0 -10 12 16 -10 -14 -8;1100:6 0 100 0 -10 12 16 -10 -14 -8;1300:" + S, "ballonroule");
            Osc("Sport", "Kayak", "-4 0 100 20 30 60 86 -10 80 -6", "-4 0 30 60 100 20 86 -10 80 -6", 0.8, 6, "baton2");

            // --- quotidien
            Cles("Quotidien", "Fait sauter une crêpe", "0:" + S + ";300:0 6 70 20 -10 12 8 -6 -8 -4;500:-4 -10 110 10 -10 12 6 0 -6 0;900:0 4 70 20 -10 12 8 -6 -8 -4;1200:" + S);
            Osc("Quotidien", "Scie une planche", "24 14 60 20 30 40 20 -24 -14 -10", "30 16 30 60 30 40 24 -30 -12 -12", 0.4, 10);
            Osc("Quotidien", "Tond la pelouse", "16 4 50 10 46 14 24 -10 -20 -20", "16 4 50 10 46 14 -20 -20 24 -10", 0.8, 6);
            Cles("Quotidien", "Souffle ses bougies", "0:" + S + ";400:-8 -10 20 20 -20 20 8 0 -8 0;700:20 20 -20 -10 -24 -10 10 -6 -8 -4;1300:20 20 -20 -10 -24 -10 10 -6 -8 -4;1600:" + S);
            Osc("Quotidien", "Enfile un pull", "0 0 170 30 -170 -30 6 0 -6 0", "6 20 150 100 -150 -100 6 0 -6 0", 0.7, 4);
            Osc("Quotidien", "Enfile une chaussure à cloche-pied", "30 20 40 30 30 40 80 -120 0 -6", "34 24 44 26 34 36 86 -126 0 -10 4", 0.6, 4);
            Osc("Quotidien", "Gonfle un pneu", "30 20 30 0 26 4 20 -30 -10 -20", "44 26 30 40 26 44 30 -50 -6 -30", 0.5, 8);
            Cles("Quotidien", "Tire sur un démarreur", "0:" + S + ";300:30 14 50 0 -10 12 20 -30 -10 -16;460:-6 -4 -50 100 -10 12 10 -8 -12 -8;700:30 14 50 0 -10 12 20 -30 -10 -16;860:-6 -4 -50 100 -10 12 10 -8 -12 -8;1100:" + S);
            Osc("Quotidien", "Secoue un shaker", "-4 -4 130 100 110 120", "-4 -4 100 130 80 150", 0.18, 16);
            Osc("Quotidien", "Étend le linge", "0 -10 160 20 150 30 6 0 -6 0", "10 10 60 30 50 40 10 -10 -6 -6", 1.0, 4);
            Cles("Quotidien", "Pique du nez debout", "0:" + S + ";800:6 20 10 12 -10 12 6 0 -6 0;1400:14 40 10 12 -10 12 8 -6 -6 -4;1500:-4 -10 30 30 -30 20 6 0 -6 0;1900:" + S + ";2600:6 22 10 12 -10 12 6 0 -6 0;3200:16 44 10 12 -10 12 8 -8 -6 -4;3300:-4 -10 30 30 -30 20 6 0 -6 0;3700:" + S, "zzz");
            Osc("Quotidien", "Cherche du réseau", "-4 -14 165 10 -8 12", "-4 -14 150 30 -8 12", 0.9, 4, "telephone");
            Osc("Quotidien", "Se mouche", "6 14 80 140 70 150", "10 20 82 142 72 152", 0.5, 4);
            Cles("Quotidien", "S'admire dans un miroir", "0:" + S + ";400:-4 -6 150 140 -10 12 6 0 -6 0;900:-6 -8 -35 105 -45 112 8 0 -8 0;1500:-8 -10 100 30 80 40 8 0 -8 0;2000:" + S);
            Osc("Quotidien", "Pousse un meuble lourd", "34 10 80 0 76 4 40 -30 -30 -10", "38 12 84 0 80 4 -30 -10 40 -30", 0.9, 5);

            // --- émotions
            Cles("Émotions", "S'évanouit", "0:" + S + ";300:-6 -20 150 120 -10 12 6 0 -6 0;700:-20 -30 150 120 -20 10 10 -10 -4 -10;1000:" + Dos + ";2400:" + Dos + ";2800:" + Cr + ";3100:" + S);
            Cles("Émotions", "Hurle au ciel", "0:" + S + ";300:-16 -34 -40 -30 -44 -30 14 -10 -14 -10;1300:-20 -40 -50 -36 -54 -36 16 -14 -16 -14;1700:" + S, "exclam");
            Osc("Émotions", "Se roule par terre de rire", "0 10 60 100 50 110 60 -80 20 -40 0 -78", "0 10 50 110 60 100 20 -40 60 -80 0 -72", 0.3, 10);
            Osc("Émotions", "Flotte sur un petit nuage", "-6 -14 50 120 40 126 10 -20 -10 -30 16", "-8 -16 54 124 44 130 14 -26 -6 -34 24", 1.4, 3, "coeur");
            Osc("Émotions", "Gêné, se frotte le bras", "4 18 30 100 0 10 6 0 -6 0", "4 20 26 112 0 10 6 0 -2 -8", 0.5, 5);
            Cles("Émotions", "Dégoûté, repousse des mains", "0:" + S + ";250:-16 -10 100 30 90 40 10 -6 -14 -8 0 0 -8;1000:-18 -12 104 26 94 36 10 -6 -14 -8 0 0 -10;1300:" + S);
            Cles("Émotions", "Émerveillé, les mains sur les joues", "0:" + S + ";300:-8 -16 110 140 100 150 8 0 -8 0;1400:-10 -20 112 142 102 152 8 0 -8 0;1700:" + S);
            Osc("Émotions", "Se frappe la poitrine", "-8 -8 60 130 -8 12 8 0 -8 0", "-10 -10 44 110 -8 12 8 0 -8 0", 0.3, 6);
            Cles("Émotions", "Claque des talons en sautant", "0:" + S + ";180:" + Cr + ";380:-10 -10 150 10 -150 -10 40 -60 30 -50 40;520:-10 -10 160 0 -160 0 44 -70 34 -60 44;700:" + Cr + ";900:" + S);
            Cles("Émotions", "Épuisé, s'affale par terre", "0:" + S + ";500:10 20 14 10 -6 10 20 -30 10 -24;900:-10 10 30 20 -30 20 86 -10 80 -6;2200:-6 24 34 16 -34 16 86 -10 80 -6;2700:" + Cr + ";3000:" + S);
            Osc("Émotions", "Curieux, regarde par le trou de la serrure", "50 -30 60 100 -20 20 30 -50 -10 -40", "54 -34 62 102 -20 20 32 -54 -10 -44", 1.2, 3);

            // --- danses
            Cles("Danses", "Chorégraphie de la Macarena", "0:" + S + ";300:0 0 90 0 -10 12 6 0 -6 0;600:0 0 90 0 86 0 6 0 -6 0;900:0 4 60 130 86 0 6 0 -6 0;1200:0 4 60 130 56 134 6 0 -6 0;1500:0 -4 150 140 56 134 6 0 -6 0;1800:0 -4 150 140 146 144 6 0 -6 0"
                + ";2100:6 0 -30 100 146 144 10 -10 -4 -6;2400:-6 0 -30 100 -36 106 4 -6 -10 -10;2700:6 0 -30 100 -36 106 10 -10 -4 -6 8;3000:" + S);
            Osc("Danses", "Charleston", "0 0 60 60 -60 60 40 -60 -10 -10", "0 0 -60 60 60 60 -10 -10 -40 -60", 0.4, 10);
            Osc("Danses", "Danse du cavalier", "6 0 60 40 56 44 30 -50 -20 -40 6", "6 0 66 30 62 34 20 -40 -30 -50", 0.3, 12);
            Osc("Danses", "Valse", "-4 0 90 20 -60 100 14 -10 -10 -6 0 0 10", "4 0 90 20 -60 100 -10 -6 14 -10 0 0 -10", 1.2, 4);
            Osc("Danses", "Kazatchok", "-6 0 40 110 30 120 95 -5 60 -130", "-6 0 40 110 30 120 60 -130 95 -5", 0.4, 10);
            Osc("Danses", "French cancan", "-6 0 -35 105 -45 112 105 -5 0 0", "-6 0 -35 105 -45 112 0 0 105 -5", 0.4, 10);
            Osc("Danses", "Sirtaki", "0 0 95 0 -95 0 30 -10 -10 -20 0 0 6", "0 0 95 0 -95 0 -10 -20 30 -10 0 0 -6", 0.5, 10);
            Osc("Danses", "Hula hawaïen", "6 0 80 20 60 40 10 -16 -6 -16 0 0 3", "-6 0 60 40 80 20 6 -16 -10 -16 0 0 -3", 0.8, 6);
            Osc("Danses", "Flamenco", "-8 -10 170 60 -30 100 10 -6 -8 -4", "-8 -10 150 100 -30 100 10 -6 20 -40", 0.4, 10);
            Osc("Danses", "Claquettes", "0 0 20 30 -20 30 20 -40 -6 -4", "0 0 -20 30 20 30 -6 -4 20 -40", 0.2, 16);
            Osc("Danses", "Pogo", "0 0 6 6 -6 6 4 0 -4 0", "0 -10 4 4 -4 4 4 0 -4 0 26", 0.3, 12);
            Osc("Danses", "Headbang", "-10 -30 60 100 30 110 14 -14 -14 -10", "20 50 60 100 30 110 14 -18 -14 -14", 0.3, 12);
            Osc("Danses", "Slow tout seul", "-4 6 40 120 30 130 8 -4 -8 -4 0 0 5", "4 6 40 120 30 130 8 -4 -8 -4 0 0 -5", 1.4, 4, "coeur");
            Osc("Danses", "Limbo", "-38 -10 -40 -20 -50 -20 40 -90 30 -86 0 0 -6", "-44 -12 -44 -20 -54 -20 44 -96 34 -92 0 0 6", 0.6, 8);
            Osc("Danses", "Running man", "10 0 60 80 -60 80 60 -80 -20 -5 0 0 -4", "10 0 -60 80 60 80 -20 -5 60 -80 0 0 4", 0.35, 12);
            Osc("Danses", "Haka", "-4 0 100 120 90 130 50 -90 40 -84", "4 0 40 100 30 110 56 -100 46 -94", 0.5, 8);

            // --- objets de Minecraft tenus en main
            const string F = "Minecraft";
            const string mange1 = "0 8 60 125 -8 12", mange2 = "0 14 70 140 -8 12", boit1 = "-6 -18 70 135 -8 12", boit2 = "-10 -28 78 142 -8 12";
            Osc(F, "Mange une part de pastèque", mange1, mange2, 0.45, 6, "item:melon_slice");
            Osc(F, "Mange une tarte à la citrouille", mange1, mange2, 0.55, 5, "item:pumpkin_pie");
            Osc(F, "Croque une pomme", mange1, mange2, 0.5, 6, "item:apple");
            Osc(F, "Mange un gâteau", mange1, mange2, 0.6, 5, "item:cake");
            Osc(F, "Boit une soupe de champignons", boit1, boit2, 0.9, 3, "item:mushroom_stew");
            Osc(F, "Boit une bouteille de miel", boit1, boit2, 1.0, 3, "item:honey_bottle");
            Cles(F, "Lance un œuf", "0:" + S + ";180:-10 -5 -150 -20 30 20 10 -8 -10 -4;330:14 5 100 5 -20 30 18 -14 -14 -4;520:" + S, "item:egg");
            Cles(F, "Lance sa ligne", "0:" + S + ";300:-10 -6 -150 -20 -10 12 10 -8 -10 -4;480:10 4 100 0 -10 12 14 -10 -12 -4;1800:8 6 80 10 -10 12 10 -6 -8 -4;2100:-6 -4 150 30 -10 12 8 -4 -8 -4;2500:" + S, "outil:fishing_rod");
            Osc(F, "Lève son épée en netherite", "-6 -10 160 10 -8 12", "-8 -14 168 0 -8 12", 0.7, 4, "outil:netherite_sword");
            Osc(F, "Danse avec un disque", "-4 -6 150 20 -40 60 14 -20 -8 -14", "4 -6 150 -10 -40 100 -8 -14 14 -20", 0.5, 8, "item:music_disc_cat").Son = "note";
            Osc(F, "Écrit dans son livre", "8 20 55 95 50 100", "8 22 57 98 46 110", 0.3, 10, "item:writable_book");
            Osc(F, "Frotte son briquet", "10 10 60 40 -8 12", "10 12 60 62 -8 12", 0.3, 8, "item:flint_and_steel");
            Osc(F, "Fait sauter un lingot d'or dans sa main", "0 0 80 70 -8 12", "0 -4 92 80 -8 12", 0.4, 8, "item:gold_ingot");
            Osc(F, "Brandit une étoile du Nether", "-4 -12 160 10 -8 12", "-6 -16 166 4 -8 12", 0.8, 4, "item:nether_star");
            Cles(F, "Lance un œil de l'Ender", "0:" + S + ";250:6 4 30 30 -10 12 10 -8 -8 -4;450:-6 -14 165 5 -10 12 6 0 -6 0;1300:-8 -22 160 10 -10 12 6 0 -6 0;1600:" + S, "item:ender_eye");
            Osc(F, "Coupe aux cisailles", "10 8 70 30 -8 12", "10 8 74 50 -8 12", 0.25, 10, "outil:shears");
            Osc(F, "Fouille à la brosse", "40 20 50 20 30 30 62 -112 52 -104", "40 20 62 10 30 30 62 -112 52 -104", 0.35, 10, "outil:brush");
            Anim carotte = Pas("Avance, une carotte au bout du bâton", 18, 30, 12, 10, 4, 0, 55, 1.0);
            Func<double, double[]> pasCarotte = carotte.Pose;
            carotte.Pose = t => { double[] p = pasCarotte(t); p[I.Ep1] = 80; p[I.Co1] = 10; return p; };
            carotte.Famille = F; carotte.Objet = "outil:carrot_on_a_stick";
            Anim poisson = Pas("Promène son poisson dans un seau", 16, 24, 10, 8, 2, 0, 36, 1.3);
            Func<double, double[]> pasPoisson = poisson.Pose;
            poisson.Pose = t => { double[] p = pasPoisson(t); p[I.Ep1] = 20; p[I.Co1] = 10; return p; };
            poisson.Famille = F; poisson.Objet = "item:tropical_fish_bucket";
        }

        // ------------------------------------------------------------ neuvième fournée : enchaînements, ralentis et nouveautés

        // Plusieurs animations bout à bout : un enchaînement de coups, une série d'acrobaties.
        static Anim Suite(string famille, params string[] noms)
        {
            var morceaux = new Anim[noms.Length];
            double total = 0;
            for (int i = 0; i < noms.Length; i++)
            {
                morceaux[i] = Trouver(noms[i]);
                if (morceaux[i] == null || morceaux[i].Nom != noms[i]) return null;      // un nom mal écrit : pas d'enchaînement bancal
                total += morceaux[i].Duree * morceaux[i].Tours;
            }
            return Ajouter(famille, string.Join(" + ", noms), total, 1, t =>
            {
                double reste = t * total;
                for (int i = 0; i < morceaux.Length; i++)
                {
                    Anim m = morceaux[i];
                    double duree = m.Duree * m.Tours;
                    if (reste < duree || i == morceaux.Length - 1)
                    {
                        double u = reste / m.Duree;
                        return m.Pose(u >= m.Tours ? 1 : u - Math.Floor(u));
                    }
                    reste -= duree;
                }
                return morceaux[0].Pose(0);
            });
        }

        // Une marche dont le bras avant tient quelque chose (ou une laisse invisible).
        static Anim PasAvec(string nom, string objet, double epaule, double coude, double A, double K, double v, double T, double penche = 2, double tete = 0)
        {
            Anim a = Pas(nom, A, K, 12, 10, penche, 0, v, T);
            Func<double, double[]> pas = a.Pose;
            a.Pose = t => { double[] p = pas(t); p[I.Ep1] = epaule; p[I.Co1] = coude; p[I.Tete] += tete; return p; };
            a.Objet = objet;
            return a;
        }

        static void Neuvieme()
        {
            // --- enchaînements de combat : deux coups, puis quelques séries de trois
            string[] coups = { "Coup de poing", "Crochet", "Uppercut", "Coup de coude", "Coup de pied", "Coup de pied haut", "Coup de genou", "Coup de pied arrière", "Balayage", "Coup de tête" };
            for (int i = 0; i < coups.Length; i++)
                for (int j = i + 1; j < coups.Length; j++)
                    Suite("Combat", coups[i], coups[j]);
            foreach (string[] serie in new[]
            {
                new[] { "Coup de poing", "Crochet", "Uppercut" }, new[] { "Coup de poing", "Coup de coude", "Coup de genou" },
                new[] { "Coup de pied", "Coup de pied haut", "Coup de pied arrière" }, new[] { "Crochet", "Uppercut", "Coup de tête" },
                new[] { "Coup de genou", "Coup de coude", "Balayage" }, new[] { "Coup de pied", "Coup de poing", "Coup de pied sauté" },
                new[] { "Uppercut", "Coup de pied haut", "Coup de pied retourné" }, new[] { "Balayage", "Coup de pied sauté", "Coup de pied marteau" },
                new[] { "Frappe de la paume", "Coup de coude", "Coup de tête" }, new[] { "Coup de poing", "Crochet", "Coup de pied retourné" },
                new[] { "Coup de pied bas", "Coup de pied", "Coup de pied haut" }, new[] { "Crochet", "Coup de genou", "Double coup de pied sauté" },
            })
                Suite("Combat", serie);

            // --- séries d'acrobaties
            string[] figures = { "Salto avant", "Salto arrière", "Roue", "Saut de mains", "Flip-flap", "Saut carpé", "Saut en étoile", "Roulade avant", "Saut groupé" };
            for (int i = 0; i < figures.Length; i++)
                for (int j = i + 1; j < figures.Length; j++)
                    Suite("Acrobaties", figures[i], figures[j]);

            // --- au ralenti : les mêmes, trois fois plus lentes, pour bien voir
            foreach (string nom in new[] { "Salto avant", "Salto arrière", "Double salto avant", "Coup de pied retourné", "Coup de pied sauté", "Roue", "Flip-flap", "Saut de l'ange",
                "Uppercut sauté", "Salto vrillé", "Triple salto", "Grand écart sauté", "Coup de pied de la grue", "Plongeon de gardien", "Saut de biche", "Salto arrière frappé" })
            {
                Anim m = Trouver(nom);
                if (m == null || m.Nom != nom) continue;
                Anim lent = Ajouter(m.Famille, nom + " au ralenti", m.Duree * 3, m.Tours, m.Pose);
                lent.Objet = m.Objet; lent.Vitesse = m.Vitesse / 3;
            }

            // --- déplacements
            Pas("Soldat de plomb", 30, 0, 30, 0, 0, 0, 55, 0.9);
            Pas("Trot de poney", 28, 90, 20, 80, 2, 6, 120, 0.42);
            Pas("Ninja accroupi", 30, 50, 0, 0, 34, 0, 150, 0.5, 2, 60, 40);
            Pas("Marche nordique", 30, 36, 60, 20, 8, 0, 95, 0.8);
            Pas("Pas de l'oie", 46, 0, 50, 0, -4, 0, 70, 1.0);
            Pas("Petite foulée", 30, 60, 30, 90, 8, 5, 170, 0.5);
            Pas("Marche du penseur", 18, 24, 0, 0, 10, 0, 30, 1.5, 5);
            Pas("Défilé, mains sur les hanches", 30, 44, 0, 0, -8, 2, 80, 0.8, 4);
            Pas("Randonneur chargé", 22, 34, 0, 0, 20, 0, 40, 1.1, 5);
            PasAvec("Promène un chien invisible", null, 50, 10, 22, 34, 60, 1.0);
            PasAvec("Marche le nez sur son téléphone", "telephone", 55, 115, 18, 26, 40, 1.2, 6, 24);
            PasAvec("Marche en lisant un livre", "livre", 55, 105, 18, 26, 36, 1.3, 6, 22);
            PasAvec("Se promène sous son parapluie", "parapluie", 60, 110, 20, 28, 45, 1.1);
            PasAvec("Défile en majorette", "baton", 150, 10, 34, 90, 70, 0.6, -4);

            // --- gestes
            Osc("Gestes", "Se gratte le dos", "0 6 170 150 -8 12", "0 6 174 140 -8 12", 0.3, 8);
            Osc("Gestes", "Shaka", "0 -4 120 60 -8 12", "0 -4 124 80 -8 12", 0.3, 6);
            Cles("Gestes", "Attrape une mouche au vol", "0:" + S + ";200:0 -6 120 20 -10 12 6 0 -6 0;320:4 0 100 90 -10 12 8 -4 -8 -4;900:6 10 70 110 -10 12 6 0 -6 0;1300:" + S);
            Osc("Gestes", "Regarde s'il pleut", "-4 -14 80 20 -8 12", "-6 -20 84 16 -8 12", 1.2, 3);
            Osc("Gestes", "Pouce vers le bas", "0 6 60 30 -8 12", "0 8 56 24 -8 12", 0.5, 4);

            // --- combat, sport
            Osc("Combat", "Garde du tigre", "20 0 100 60 80 80 40 -60 -30 -20", "24 2 104 56 84 76 44 -66 -30 -24", 1.2, 3);
            Cles("Sport", "Lance un frisbee", "0:" + S + ";300:-10 0 40 110 -10 12 14 -14 -12 -8;480:10 0 100 0 -30 20 20 -16 -18 -6;800:" + S);
            Cles("Sport", "Trampoline", "0:" + S + ";200:" + Cr + ";450:-4 -10 150 20 -150 -20 8 0 -8 0 50;700:" + Cr + ";950:0 0 160 0 -160 0 60 0 -60 0 70;1200:" + Cr
                + ";1450:" + Boule + " 80 180;1700:-4 -10 150 20 -150 -20 8 0 -8 0 40 360;1900:" + Cr + " 0 360;2100:" + S + " 0 360");
            Osc("Sport", "Étire ses mollets", "20 0 80 0 76 4 30 -30 -40 0", "24 0 82 0 78 4 34 -36 -42 0", 1.5, 3);
            Osc("Sport", "Slalom à ski", "30 -10 -30 60 -34 60 50 -90 46 -86 0 0 8", "30 -10 -30 60 -34 60 60 -100 56 -96 0 0 -8", 0.6, 8, "planche");
            Osc("Sport", "Rodéo", "-20 -10 160 20 40 60 60 -70 50 -64 6 0 4", "20 10 140 -20 40 60 70 -80 60 -74 0 0 -4", 0.35, 12);

            // --- quotidien
            Osc("Quotidien", "Range en haut d'une étagère", "-4 -14 170 10 160 20 6 0 -6 0 4", "0 0 80 30 70 40 8 -6 -8 -4", 1.0, 4);
            Bruit(Osc("Quotidien", "Frappe à une porte", "4 0 90 100 -8 12", "4 0 96 70 -8 12", 0.2, 8), "coup", 0.2);
            Cles("Quotidien", "Appelle l'ascenseur", "0:" + S + ";300:4 0 80 10 -10 12 6 0 -6 0;500:4 0 84 4 -10 12 6 0 -6 0;700:" + S + ";1500:-4 -16 10 12 -10 12 6 0 -6 0;2300:" + S);
            Cles("Quotidien", "Arrache une mauvaise herbe", "0:" + S + ";400:40 20 40 10 36 14 62 -112 52 -104;700:44 22 44 6 40 10 62 -112 52 -104;900:-10 -6 60 60 56 64 30 -40 20 -30 6;1100:-14 -8 50 80 46 84 20 -20 -10 -10 0 0 -6;1500:" + S);
            Cles("Quotidien", "Se brûle avec son café", "0:" + S + ";400:0 6 70 125 -10 12 6 0 -6 0;700:0 10 74 132 -10 12 6 0 -6 0;850:-10 -10 40 60 -30 20 8 -6 -8 -4;1300:-6 -4 60 80 -10 12 6 0 -6 0;1700:" + S, "exclam");

            // --- émotions
            Osc("Émotions", "L'air innocent, sifflote", "-6 -16 -30 -20 -34 -20 6 0 -6 0", "-4 -20 -30 -20 -34 -20 6 0 10 -14", 0.8, 4, "note");
            Osc("Émotions", "Prie pour que ça marche", "10 20 70 110 66 114 6 0 -6 0", "14 26 72 112 68 116 6 0 -6 0", 1.4, 3);
            Cles("Émotions", "Se fait tout petit", "0:" + S + ";400:30 30 60 130 56 134 70 -130 60 -124;1500:34 34 62 132 58 136 74 -134 64 -128;1900:" + S);
            Osc("Émotions", "Exaspéré, lève les yeux au ciel", "-6 -30 -35 105 -45 112 6 0 -6 0", "-2 -6 -35 105 -45 112 6 0 -6 0", 1.2, 3);
            Osc("Émotions", "Se retient, danse d'un pied sur l'autre", "10 10 30 60 26 64 14 -30 -4 -20 0 0 2", "10 10 30 60 26 64 -4 -20 14 -30 0 0 -2", 0.2, 16);
            Osc("Émotions", "S'époussette les mains, travail fini", "0 -6 60 70 50 80 6 0 -6 0", "0 -6 50 80 60 70 6 0 -6 0", 0.25, 8);

            // --- objets de Minecraft tenus en main
            const string F = "Minecraft";
            const string mange1 = "0 8 60 125 -8 12", mange2 = "0 14 70 140 -8 12", boit1 = "-6 -18 70 135 -8 12", boit2 = "-10 -28 78 142 -8 12";
            foreach (string[] m in new[]
            {
                new[] { "Mange un poulet rôti", "cooked_chicken" }, new[] { "Mange une pomme de terre cuite", "baked_potato" }, new[] { "Mange une côtelette", "cooked_porkchop" },
                new[] { "Mange un saumon grillé", "cooked_salmon" }, new[] { "Mange du mouton rôti", "cooked_mutton" }, new[] { "Mange une morue grillée", "cooked_cod" },
                new[] { "Grignote des baies sucrées", "sweet_berries" }, new[] { "Grignote des baies lumineuses", "glow_berries" }, new[] { "Mâchouille du varech séché", "dried_kelp" },
                new[] { "Croque une betterave", "beetroot" }, new[] { "Croque une carotte", "carrot" },
            })
                Osc(F, m[0], mange1, mange2, 0.5, 6, "item:" + m[1]);
            Osc(F, "Mange un ragoût de lapin", boit1, boit2, 0.9, 3, "item:rabbit_stew");
            Osc(F, "Goûte une soupe suspecte", boit1, boit2, 1.1, 3, "item:suspicious_stew");
            foreach (string[] m in new[]
            {
                new[] { "Admire un éclat d'améthyste", "amethyst_shard" }, new[] { "Admire un cœur de la mer", "heart_of_the_sea" },
                new[] { "Admire un lingot de netherite", "netherite_ingot" }, new[] { "Admire une perle de l'Ender", "ender_pearl" },
            })
                Osc(F, m[0], "0 -8 110 60 -8 12", "0 -12 118 66 -8 12", 0.8, 4, "item:" + m[1]);
            Osc(F, "Écoute la mer dans un coquillage", "4 6 150 125 -8 12", "6 10 152 127 -8 12", 1.4, 3, "item:nautilus_shell");
            Osc(F, "Se chatouille avec une plume", "0 8 100 150 -8 12", "0 4 104 140 -8 12", 0.25, 10, "item:feather");
            Osc(F, "Fait rebondir une boule de slime", "0 0 70 60 -8 12", "0 -4 96 70 -8 12", 0.35, 8, "item:slime_ball");
            Osc(F, "Porte un seau de lave à bout de bras", "-10 -8 86 0 -8 12 8 -4 -8 -4", "-12 -10 90 0 -8 12 10 -8 -6 -2", 0.4, 6, "item:lava_bucket");
            Osc(F, "Sème des graines", "10 8 50 20 -8 12", "14 10 90 10 -8 12", 0.7, 5, "item:wheat_seeds");
            Osc(F, "Montre sa récolte de blé", "-4 -10 150 10 -8 12", "-6 -14 158 4 -8 12", 0.8, 4, "item:wheat");
            Osc(F, "Saupoudre de la redstone", "10 10 70 30 -8 12", "10 12 74 50 -8 12", 0.3, 8, "item:redstone");
            Osc(F, "Répand de la poudre d'os", "14 12 60 20 -8 12", "14 14 64 44 -8 12", 0.35, 8, "item:bone_meal");
            Osc(F, "Lit une feuille de papier", "6 18 60 105 -8 12", "6 22 62 110 -8 12", 1.3, 3, "item:paper");
            Osc(F, "Regarde au fond de son seau", "14 30 50 80 -8 12", "16 36 52 84 -8 12", 1.2, 3, "item:bucket");
            foreach (string[] m in new[] { new[] { "Mine avec une pioche en fer", "iron_pickaxe" }, new[] { "Mine avec une pioche en or", "golden_pickaxe" }, new[] { "Mine avec une pioche en netherite", "netherite_pickaxe" } })
                Bruit(Osc(F, m[0], "-6 -5 150 40 -10 12 12 -8 -10 -4", "14 6 45 5 -10 12 16 -12 -12 -4", 0.5, 8, "outil:" + m[1]), "coup", 0.45);
            foreach (string[] m in new[] { new[] { "Fend l'air avec une épée en fer", "iron_sword" }, new[] { "Fend l'air avec une épée en or", "golden_sword" } })
                Bruit(Osc(F, m[0], "-10 -5 160 30 55 110 14 -15 -14 -10", "22 5 40 0 50 115 24 -22 -22 -6 0 0 8", 0.5, 6, "outil:" + m[1]), "swish", 0.3);
            Bruit(Osc(F, "Frappe le sol avec une masse", "-12 -8 170 10 60 100 20 -30 -10 -30 16", "26 8 50 0 50 115 40 -70 20 -60", 0.7, 5, "outil:mace"), "coup", 0.5);
            Osc(F, "Agite un bâton de blaze", "0 -6 150 20 -8 12", "0 -6 150 -30 -8 12", 0.4, 6, "outil:blaze_rod");
        }

        // ------------------------------------------------------------ dixième fournée

        static Anim AuBord(Anim a) { a.SurFenetre = true; return a; }

        static void Dixieme()
        {
            // --- perché sur une fenêtre, les jambes dans le vide
            const string jambes1 = " 22 -4 4 -32 -44", jambes2 = " 4 -32 22 -4 -44", pose = "-8 0 -25 -10 -35 -10";
            AuBord(Osc("Fenêtres", "Assis au bord, lit un livre", "-4 16 55 95 50 100" + jambes1, "-4 20 57 98 52 97" + jambes2, 2.0, 4, "livre"));
            AuBord(Osc("Fenêtres", "Assis au bord, joue de la guitare", "-6 0 -10 80 -50 -70" + jambes1, "-6 4 -10 118 -50 -66" + jambes1, 0.3, 12, "guitare")).Son = "note";
            AuBord(Osc("Fenêtres", "Assis au bord, casse la croûte", "0 8 60 125 -35 -10" + jambes1, "0 14 70 140 -35 -10" + jambes2, 0.6, 6));
            AuBord(Osc("Fenêtres", "Assis au bord, fait coucou en bas", "20 20 100 20 -35 -10" + jambes1, "20 20 100 70 -35 -10" + jambes1, 0.4, 6));
            AuBord(Osc("Fenêtres", "Assis au bord, pianote sur son téléphone", "-4 20 55 115 -35 -10" + jambes1, "-4 22 57 112 -35 -10" + jambes1, 0.2, 14, "telephone"));
            AuBord(Osc("Fenêtres", "Assis au bord, s'étire", pose + jambes1, "-16 -16 160 20 -160 -20 30 -4 10 -20 -44", 2.0, 3));
            AuBord(Osc("Fenêtres", "Assis au bord, pique du nez", "10 30 -20 -10 -30 -10 22 -10 8 -30 -44", "14 42 -20 -10 -30 -10 22 -10 8 -30 -44", 2.4, 4, "zzz"));
            AuBord(Cles("Fenêtres", "Assis au bord, lance des cailloux", "0:" + pose + jambes1 + ";300:-14 -6 -150 -20 -35 -10" + jambes1 + ";450:6 4 100 5 -35 -10" + jambes1 + ";900:" + pose + jambes1));
            AuBord(Osc("Fenêtres", "Guette l'horizon du haut de sa fenêtre", "-4 -10 140 120 -10 12 6 0 -6 0", "-6 -6 142 122 -10 12 6 0 -6 0", 1.6, 3));

            // --- déplacements
            Pas("Marathonien épuisé", 30, 50, 20, 70, 22, 2, 90, 0.7);
            Pas("Patine à reculons", 30, 14, 40, 20, 10, 0, -150, 1.2);
            Pas("Marche sur des braises", 20, 100, 30, 60, -6, 8, 60, 0.28, 3);
            Anim pirate = Ajouter("Déplacements", "Pirate à jambe de bois", 1.1, 1, t =>
            {
                double f = 2 * Math.PI * t, s = Math.Sin(f), c = Math.Cos(f);
                double[] p = L("6 0 0 20 0 20 0 0 0 0");
                p[I.Ha1] = 24 * s; p[I.Ha2] = -24 * s; p[I.Ge2] = -44 * Math.Max(0, -c); p[I.Torse] = 6 + 7 * s; p[I.Ep1] = -18 * s; p[I.Ep2] = 18 * s;
                return p;
            });
            pirate.Deplace = true; pirate.Vitesse = 34;
            Anim fantome = Ajouter("Déplacements", "Flotte comme un fantôme", 1.6, 1, t =>
            {
                double w = Math.Sin(2 * Math.PI * t);
                double[] p = L("10 0 80 30 76 34 -10 -20 -16 -24 12");
                p[I.Air] = 12 + 5 * w; p[I.Co1] = 30 + 14 * w; p[I.Co2] = 34 - 14 * w;
                return p;
            });
            fantome.Deplace = true; fantome.Vitesse = 50;

            // --- gestes
            Osc("Gestes", "Hèle un taxi", "-4 -8 160 0 -8 12", "-6 -10 150 20 -8 12", 0.5, 5);
            Osc("Gestes", "Main sur le cœur", "6 10 40 120 -8 12", "10 16 42 122 -8 12", 1.4, 3);
            Cles("Gestes", "Robot en panne", "0:" + S + ";300:0 0 90 90 -10 12 6 0 -6 0;600:0 0 90 90 -90 -90 6 0 -6 0;900:0 20 90 0 -90 0 6 0 -6 0;1200:30 40 20 10 10 14 10 -10 -6 -6;2200:34 44 22 10 12 14 10 -10 -6 -6;2600:" + S);
            Cles("Gestes", "Tour de magie", "0:" + S + ";400:6 6 20 110 -10 12 6 0 -6 0;600:0 -6 110 20 -10 12 6 0 -6 0;1400:0 -8 114 16 -10 12 6 0 -6 0;1800:" + S, "exclam");
            Osc("Gestes", "Fait craquer son cou", "0 -22 -35 105 -45 112", "0 22 -35 105 -45 112", 0.9, 4);
            Osc("Gestes", "Tend la main", "20 10 70 10 -8 12", "24 12 74 6 -8 12", 1.0, 3);

            // --- combat, acrobaties : une frappe et un saut de plus, et des suites qui commencent par une esquive
            Cles("Combat", "Double uppercut", "0:" + G + ";180:22 5 10 95 55 110 30 -45 -10 -30;320:-6 -12 140 45 50 115 14 -8 -14 -4 6 0 4;460:22 5 55 110 10 95 30 -45 -10 -30;600:-6 -12 50 115 140 45 14 -8 -14 -4 6 0 4;800:" + G);
            Suite("Combat", "Roulade d'esquive", "Uppercut");
            Suite("Combat", "Esquive basse", "Uppercut");
            Suite("Combat", "Esquive arrière", "Coup de pied");
            Suite("Combat", "Roulade d'esquive", "Balayage");
            Suite("Combat", "Provocation", "Esquive arrière");
            Cles("Acrobaties", "Saute par-dessus sa jambe", "0:" + S + ";300:20 10 30 10 -10 12 80 -100 0 -6;600:24 12 34 6 -10 12 84 -104 0 -30;760:10 0 34 6 -10 12 84 -104 60 -120 26;920:20 10 30 10 -10 12 80 -100 0 -10;1300:" + S);
            Suite("Acrobaties", "Saut de l'ange", "Roulade avant");
            Suite("Acrobaties", "Poirier", "Roulade avant");
            Suite("Acrobaties", "Rondade flip", "Salto arrière");
            Suite("Acrobaties", "Kip-up", "Salto avant");
            Suite("Acrobaties", "Roue", "Grand écart");

            // --- sport
            Cles("Sport", "Frappe de baseball", "0:" + S + ";300:-14 0 -60 110 -70 100 16 -16 -14 -10;480:16 0 110 20 100 30 24 -20 -22 -6 0 0 6;800:" + S);
            Osc("Sport", "Curling", "40 20 60 20 40 60 30 -40 -20 -20", "44 22 80 10 60 50 30 -40 -20 -20", 0.25, 12, "balai");
            Osc("Sport", "Billard", "50 -30 100 0 -40 -20 20 -20 -30 -6", "50 -30 100 0 -60 -40 20 -20 -30 -6", 0.8, 4, "baton2");
            Osc("Sport", "Ping-pong", "14 0 60 60 -20 30 20 -24 -18 -14 0 0 4", "14 0 100 20 -20 30 16 -18 -22 -18 0 0 -4", 0.3, 12, "raquette");
            Osc("Sport", "Soulevé de terre", "50 10 20 0 16 0 60 -100 50 -94", "0 0 6 4 2 4 6 0 -6 0", 1.4, 5, "haltere");

            // --- quotidien
            Osc("Quotidien", "Fait un château de sable", "30 20 60 10 50 20 0 -95 0 -95", "40 26 40 30 30 40 0 -95 0 -95", 0.6, 6);
            Osc("Quotidien", "Flotte sur le dos", "0 10 100 0 -100 0 15 0 12 0 0 -80", "0 10 120 0 -120 0 20 0 8 0 2 -78", 2.0, 3);
            Cles("Quotidien", "Se réveille en sursaut", "0:" + Dos + ";800:" + Dos + ";950:-20 -10 30 30 -30 20 86 -10 80 -6;1500:-16 -6 60 110 -30 20 86 -10 80 -6;2000:" + Cr + ";2300:" + S, "exclam");
            Cles("Quotidien", "Regarde sa montre et panique", "0:" + S + ";400:4 18 60 120 -10 12 6 0 -6 0;1000:4 18 60 120 -10 12 6 0 -6 0;1150:-10 -16 150 30 -150 -30 10 -10 -10 -10 8;1500:-8 -10 140 40 -140 -40 8 -6 -8 -6;1800:" + S, "exclam");
            Osc("Quotidien", "Passe la serpillière", "24 12 60 10 40 40 14 -12 -10 -8 0 0 6", "24 12 30 20 10 50 12 -10 -12 -10 0 0 -6", 0.9, 5, "balai");
            // assis sur un bloc, comme sur une marche : le temps que ça cuise
            Osc("Quotidien", "Assis sur un bloc, s'ennuie", "30 40 30 60 20 70 80 -80 74 -76 22", "34 46 32 62 22 72 80 -80 74 -76 22", 2.2, 3, "assise");
            Osc("Quotidien", "Assis sur un bloc, le menton dans la main", "20 10 60 130 20 70 80 -80 74 -76 22", "22 14 62 132 20 70 80 -80 74 -76 22", 2.0, 3, "assise");
            Osc("Quotidien", "Assis sur un bloc, balance les pieds", "-6 0 -20 -10 -30 -10 70 -60 60 -90 18", "-6 2 -20 -10 -30 -10 60 -90 70 -60 18", 0.8, 6, "assise");

            // --- émotions
            Cles("Émotions", "Bondit comme un cabri", "0:" + S + ";150:" + Cr + ";350:-10 -10 150 20 -150 -20 40 -60 -30 -40 30 0 10;550:" + Cr + " 0 0 14;750:10 -10 -150 -20 150 20 -30 -40 40 -60 30 0 4;950:" + Cr + ";1150:" + S);
            Cles("Émotions", "Tombe à genoux, désespéré", "0:" + S + ";400:-10 -30 150 20 -150 -20 6 0 -6 0;800:0 -20 160 10 -160 -10 0 -95 0 -95;1800:10 30 20 10 -20 10 0 -95 0 -95;2300:" + Cr + ";2600:" + S);
            Osc("Émotions", "Méfiant, jette des coups d'œil", "10 -24 40 110 30 120 14 -20 -10 -20", "10 24 40 110 30 120 14 -20 -10 -20", 0.8, 4);
            Osc("Émotions", "Essuie une larme", "4 14 110 150 -8 12", "6 18 112 146 -8 12", 0.8, 4);
        }

        // ------------------------------------------------------------ onzième fournée : autour du repas, et d'autres

        static void Onzieme()
        {
            // --- déplacements
            Pas("Apporte un plat à bout de bras", 20, 28, 0, 0, -4, 0, 45, 1.1, 1);
            Pas("Pousse un chariot", 26, 34, 0, 0, 14, 0, 60, 0.9, 1);
            Pas("Marche sur les talons", 16, 6, 30, 10, -10, 0, 40, 0.8);
            Pas("Court après son chapeau", 44, 90, 0, 0, 30, 5, 250, 0.42, 1);
            PasAvec("S'en va à reculons en saluant", null, 150, 30, 22, 30, -55, 1.0);

            // --- gestes
            Osc("Gestes", "Chasse la fumée de la main", "-6 -6 100 60 -8 12", "-6 -6 110 20 -8 12", 0.2, 12);
            Osc("Gestes", "Renifle une bonne odeur", "6 -20 10 12 -10 12", "10 -30 10 12 -10 12", 0.5, 5);
            Osc("Gestes", "Fait signe que non, des deux mains", "0 0 80 40 70 50", "0 0 80 80 70 90", 0.3, 8);
            Osc("Gestes", "Se tourne les pouces", "0 10 40 100 34 106", "0 10 42 98 32 108", 0.25, 10);
            Cles("Gestes", "Salue le public", "0:" + S + ";400:30 20 100 0 -100 0 10 -6 -10 -4;1000:34 24 104 0 -104 0 10 -6 -10 -4;1400:" + S);
            Osc("Gestes", "Coup de coude complice", "-4 0 -30 100 -8 12", "-8 0 -50 90 -8 12", 0.3, 4);

            // --- quotidien : en cuisine et à table
            Cles("Quotidien", "Se brûle la langue", "0:" + S + ";400:0 8 60 125 -10 12 6 0 -6 0;650:0 12 66 134 -10 12 6 0 -6 0;800:-10 -14 100 30 90 40 8 -6 -8 -4;1000:-8 -10 110 60 100 70 8 -6 -8 -4;1200:-10 -14 100 30 90 40 8 -6 -8 -4;1600:" + S, "exclam");
            Osc("Quotidien", "Souffle sur sa soupe", "6 14 60 110 50 120", "8 18 60 110 50 120", 0.6, 5);
            Cles("Quotidien", "Met la table", "0:" + S + ";300:10 6 60 20 -10 12 10 -6 -8 -4;600:20 12 50 10 -10 12 16 -12 -10 -4 0 0 8;900:10 6 60 20 -10 12 10 -6 -8 -4;1200:20 12 50 10 -10 12 16 -12 -10 -4 0 0 -8;1500:" + S);
            Osc("Quotidien", "Épluche des légumes", "8 20 50 100 44 90", "8 20 56 90 44 90", 0.25, 12);
            Anim touille = Ajouter("Quotidien", "Touille la marmite", 0.9, 5, t => { double f = 2 * Math.PI * t; double[] p = L("10 14 50 60 -10 12"); p[I.Ep1] = 50 + 12 * Math.Cos(f); p[I.Co1] = 60 + 22 * Math.Sin(f); return p; });
            touille.Haut = true;
            Cles("Quotidien", "Ouvre un cadeau", "0:" + S + ";400:10 16 50 80 44 86 6 0 -6 0;700:10 16 70 60 20 110 6 0 -6 0;1000:10 16 50 80 44 86 6 0 -6 0;1300:-10 -14 150 20 -150 -20 8 -6 -8 -6 10;1700:" + S);
            Osc("Quotidien", "Zappe avec la télécommande", "-4 4 80 20 -8 12", "-4 4 80 40 -8 12", 0.5, 5, "telephone");
            Bruit(Osc("Quotidien", "Accroche un tableau", "-4 -14 160 20 100 60", "-4 -14 160 20 110 30", 0.4, 6), "coup", 0.5);
            Osc("Quotidien", "Souffle dans ses mains", "6 16 80 130 70 140", "8 20 82 132 72 142", 1.0, 4);
            Osc("Quotidien", "Assis sur un bloc, s'étire", "-10 -20 150 30 -150 -30 80 -80 74 -76 22", "-4 0 30 40 -30 20 80 -80 74 -76 22", 2.0, 3, "assise");
            Osc("Quotidien", "Assis sur un bloc, tape du pied", "10 6 40 110 30 120 80 -80 74 -76 22", "10 6 40 110 30 120 80 -80 82 -68 22", 0.3, 10, "assise");

            // --- émotions
            Cles("Émotions", "Affamé, se tient le ventre", "0:" + S + ";400:14 20 30 110 20 120 8 -6 -8 -4;1400:18 24 32 112 22 122 8 -8 -8 -6;1800:" + S);
            Osc("Émotions", "Repu, se tapote le ventre", "-12 -8 40 100 -20 10", "-12 -8 30 110 -20 10", 0.4, 6);
            Cles("Émotions", "Siffle d'admiration", "0:" + S + ";300:-10 -16 30 30 -30 20 8 0 -8 0;1200:-12 -20 34 26 -34 16 8 0 -8 0;1500:" + S, "note");
            Osc("Émotions", "Souffle sur les flammes, paniqué", "30 10 60 20 50 30 30 -40 -10 -20", "36 14 60 20 50 30 34 -46 -10 -24", 0.3, 10, "exclam");

            // --- sport
            Osc("Sport", "Cambre le dos", "-30 -20 -35 105 -45 112 8 0 -8 0", "4 4 -35 105 -45 112 8 0 -8 0", 1.4, 4);
            Osc("Sport", "Gardien sur sa ligne", "20 0 40 20 -40 -20 40 -60 -30 -50 0 0 6", "20 0 60 10 -60 -10 44 -66 -34 -56 0 0 -6", 0.5, 6);
            Cles("Sport", "Sort un carton rouge", "0:" + S + ";300:6 0 30 110 -10 12 6 0 -6 0;500:-4 -10 170 0 -10 12 6 0 -6 0;1300:-4 -10 170 0 -10 12 6 0 -6 0;1600:" + S, "exclam");
            Osc("Sport", "Échauffe ses poignets", "0 0 60 80 50 90", "0 0 64 70 46 100", 0.3, 8);

            // --- suites
            Suite("Combat", "Double uppercut", "Coup de pied retourné");
            Suite("Combat", "Frappe de la paume", "Coup de pied marteau");
            Suite("Combat", "Coup de genou sauté", "Coup de coude");
            Suite("Acrobaties", "Saut de biche", "Grand écart sauté");
            Suite("Acrobaties", "Salto vrillé", "Saut carpé");
            Suite("Acrobaties", "Deux roues enchaînées", "Salto arrière");

            // --- objets de Minecraft
            const string F = "Minecraft";
            const string mange1 = "0 8 60 125 -8 12 6 0 -6 0", mange2 = "0 14 70 140 -8 12 6 0 -6 0";
            Osc(F, "Croque une pomme dorée enchantée", "0 8 60 125 -8 12", "0 14 70 140 -8 12", 0.5, 6, "item:enchanted_golden_apple");
            Cles(F, "Mange un poisson-globe (mauvaise idée)", "0:" + S + ";400:" + mange1 + ";700:" + mange2 + ";1000:" + mange1 + ";1400:20 30 30 110 20 120 10 -10 -8 -8;2500:24 36 32 112 22 122 12 -14 -8 -10;3000:" + S, "item:pufferfish");
            Osc(F, "Agite un os", "0 -6 150 20 -8 12", "0 -6 150 -30 -8 12", 0.4, 6, "outil:bone");
            Osc(F, "S'éclaire à la bougie", "4 4 80 30 -8 12", "6 8 82 34 -8 12", 1.6, 3, "item:candle");
        }

        // ------------------------------------------------------------ douzième fournée : l'école, les jeux, les animaux, dehors

        static void Douzieme()
        {
            // --- à l'école
            Osc("Quotidien", "Écrit au tableau", "-4 -10 150 20 -8 12", "-4 -10 130 40 -8 12", 0.4, 8, "crayon");
            Osc("Quotidien", "Efface le tableau", "-4 -8 160 10 -8 12", "-4 -8 110 30 -8 12", 0.5, 6);
            Osc("Quotidien", "Fait tourner son stylo", "0 6 60 100 -8 12", "0 6 64 96 -8 12", 0.2, 12, "crayon");
            Osc("Quotidien", "Assis sur un bloc, s'endort", "30 50 30 60 20 70 80 -80 74 -76 22", "38 64 32 62 22 72 80 -80 74 -76 22", 2.6, 3, "assise");
            Pas("Cartable trop lourd", 20, 28, 6, 10, 24, 0, 35, 1.2);
            Pas("File à la récré", 50, 100, 60, 90, 20, 8, 300, 0.38);

            // --- les jeux vidéo
            Osc("Quotidien", "Manette en main", "10 14 50 80 44 86", "14 18 54 76 40 90", 0.3, 10);
            Cles("Émotions", "Rage quit", "0:" + S + ";400:10 14 50 80 44 86 6 0 -6 0;1200:12 16 52 78 42 88 6 0 -6 0;1400:-10 -10 160 20 150 30 8 -4 -8 -4;1600:30 10 60 0 50 10 20 -20 -14 -8;2000:-6 -14 -30 100 -35 105 6 0 -6 0;2600:" + S, "exclam");

            // --- les animaux
            Osc("Gestes", "Imite un singe", "20 0 60 150 -60 -150 30 -50 20 -44 4", "24 4 30 150 -30 -150 36 -60 26 -54", 0.3, 10);
            Osc("Gestes", "Imite un flamant rose", "0 -10 -35 105 -45 112 0 0 70 -140", "2 -12 -35 105 -45 112 0 0 72 -142", 1.8, 3);
            Pas("Marche du gorille", 24, 30, 30, 0, 40, 0, 45, 1.0);
            Anim kangourou = Ajouter("Déplacements", "Bonds de kangourou", 0.5, 1, t =>
            {
                double u = Math.Max(0, Math.Sin(2 * Math.PI * t));
                double[] p = L("20 -10 60 110 56 114 40 -70 36 -66");
                p[I.Air] = 26 * u; p[I.Ha1] = 40 - 26 * u; p[I.Ge1] = -70 + 56 * u; p[I.Ha2] = 36 - 26 * u; p[I.Ge2] = -66 + 56 * u;
                return p;
            });
            kangourou.Deplace = true; kangourou.Vitesse = 150;

            // --- dehors
            Osc("Quotidien", "Montre les étoiles, allongé", "0 10 150 0 28 -5 15 0 12 0 0 -75", "0 10 140 10 28 -5 15 0 12 0 0 -75", 1.6, 3);
            Cles("Quotidien", "Fait des ricochets", "0:" + S + ";350:20 6 -80 -10 -10 12 30 -40 -20 -16;520:24 8 60 0 -10 12 34 -46 -20 -12;900:10 0 100 10 -10 12 14 -10 -12 -8;1300:" + S);
            Cles("Quotidien", "Cueille un fruit", "0:" + S + ";400:-4 -14 172 4 -10 12 4 0 -4 0 6;900:-4 -16 176 0 -10 12 4 0 -4 0 8;1100:0 6 60 110 -10 12 6 0 -6 0;1600:" + S);
            Osc("Quotidien", "Souffle sur un pissenlit", "10 10 80 120 -8 12", "14 14 80 120 -8 12", 0.9, 3);

            // --- sport, émotions
            Cles("Sport", "Saut de précision", "0:" + S + ";250:30 10 -60 -10 -70 -10 60 -100 50 -92;450:-10 -10 150 10 140 15 70 -30 60 -24 36;650:30 10 80 10 70 15 86 -110 76 -104;1100:34 12 84 6 74 11 88 -114 78 -108;1400:" + S);
            Cles("Sport", "Yoga : salutation au soleil", "0:" + S + ";500:-6 -10 170 10 166 14 6 0 -6 0;1100:80 30 20 0 16 0 6 0 -6 0;1700:" + Planche + ";2300:0 -30 40 0 44 0 -10 0 -6 0 0 78;2900:80 30 20 0 16 0 6 0 -6 0;3500:-6 -10 170 10 166 14 6 0 -6 0;4000:" + S);
            Osc("Émotions", "Outré, les poings sur les hanches", "-8 -10 -35 105 -45 112 10 0 -10 0", "-4 -4 -35 105 -45 112 10 0 -10 0", 0.5, 5, "exclam");
            Osc("Émotions", "Ricane dans sa main", "10 16 100 150 -8 12", "14 20 102 148 -8 12", 0.2, 12);
        }

        // ------------------------------------------------------------ sixième fournée

        static void Sixieme()
        {
            // --- gestes
            Osc("Gestes", "Pouces en l'air des deux mains", "0 -6 95 20 -95 -20", "0 -8 102 28 -102 -28", 0.5, 5);
            Osc("Gestes", "Fait le signe du cœur avec les doigts", "0 4 110 130 -110 -130", "0 6 114 134 -114 -134", 0.8, 4, "coeur");
            Osc("Gestes", "Se cache les yeux", "0 10 125 140 -125 -140", "0 12 127 142 -127 -142", 1.2, 3);
            Osc("Gestes", "Joue du tambour", "8 6 60 60 55 110", "8 8 55 110 60 60", 0.3, 10);
            Osc("Gestes", "Lance des confettis", "0 -8 160 20 -160 -20", "0 -4 100 60 -100 -60", 0.5, 5, "note");
            Osc("Gestes", "Fait l'avion", "0 0 90 0 -90 0", "8 0 104 0 -76 0", 1.0, 4);
            Osc("Gestes", "Compte jusqu'à trois", "0 0 110 100 -8 12", "0 -2 120 60 -8 12", 0.6, 3);
            Osc("Gestes", "Fait un cadre avec ses mains", "4 -4 100 110 95 115", "4 -6 108 104 103 109", 1.2, 3);
            Osc("Gestes", "Moulinet du poignet", "0 0 80 70 -8 12", "0 0 84 110 -8 12", 0.22, 12);
            Osc("Gestes", "Lève les bras au ciel", "-4 -14 172 4 -172 -4", "-6 -18 176 -4 -176 4", 0.9, 3);
            Osc("Gestes", "Désigne quelqu'un du doigt", "6 0 92 0 -8 12", "8 2 96 4 -8 12", 0.5, 4);
            Osc("Gestes", "Balaye de la main", "0 0 110 20 -8 12", "0 0 60 40 -8 12", 0.5, 4);

            // --- combat
            Cles("Combat", "Enchaînement gauche-droite-uppercut", "0:" + G + ";140:16 0 90 0 50 115 22 -20 -20 -6 0 0 6;280:" + G + ";420:14 0 55 110 88 0 22 -20 -20 -6 0 0 6;560:" + G
                + ";720:20 6 30 100 55 110 30 -40 -18 -20;860:-8 -12 170 10 55 110 16 -10 -16 -6 10;1100:" + G);
            Cles("Combat", "Roulade d'esquive", "0:" + G + ";200:" + Cr + ";420:" + Boule + " 0 120;640:" + Boule + " 0 240;860:" + Cr + " 0 360;1050:" + G + " 0 360");
            Osc("Combat", "Moulinets de poings", "10 0 70 80 60 100 16 -18 -18 -10", "10 0 60 100 70 80 16 -18 -18 -10", 0.18, 16);
            Cles("Combat", "Charge de l'épaule", "0:" + G + ";200:30 10 20 100 30 90 40 -50 -20 -10;380:36 12 10 110 20 100 30 -20 -40 -5 0 0 18;600:20 6 30 90 40 80 20 -20 -20 -10 0 0 10;850:" + G);
            Cles("Combat", "Salut du combattant", "0:" + S + ";300:0 0 60 110 60 110 6 0 -6 0;700:25 20 60 110 60 110 6 0 -6 0;1200:25 20 60 110 60 110 6 0 -6 0;1600:" + S);
            Osc("Combat", "Shadow-boxing en sautillant", "10 0 92 0 55 115 16 -18 -18 -10 5", "10 0 60 110 88 0 -18 -10 16 -18", 0.28, 14);

            // --- acrobaties
            Cles("Acrobaties", "Flip arrière groupé", "0:" + S + ";200:" + Cr + ";400:" + Boule + " 60 -120;600:" + Boule + " 70 -240;800:" + Cr + " 10 -360;1000:" + S + " 0 -360");
            Cles("Acrobaties", "Saut de l'ange", "0:" + S + ";200:" + Cr + ";450:-10 -20 100 0 -100 0 -10 0 -20 0 56 20;700:-6 -14 100 0 -100 0 -6 0 -14 0 40 10;900:" + Cr + ";1100:" + S);
            Cles("Acrobaties", "Saut groupé-tendu", "0:" + S + ";180:" + Cr + ";380:" + Boule + " 50;560:0 -6 170 0 -170 0 4 0 -4 0 46;760:" + Cr + ";950:" + S);
            Fixe("Acrobaties", "Planche faciale", "0 -20 70 0 74 0 0 0 -4 0 30 88", 3);
            Cles("Acrobaties", "Saut de main", "0:" + S + ";200:30 10 150 0 160 0 30 -30 -20 0;450:0 0 180 0 176 0 20 0 -30 0 0 180;700:-20 0 150 0 160 0 40 -40 10 -20 0 290;900:" + Cr + " 0 360;1100:" + S + " 0 360");

            // --- sport
            Osc("Sport", "Saute à la corde invisible", "0 0 30 60 -30 -60 6 -10 -6 -10 12", "0 0 34 66 -34 -66 6 0 -6 0", 0.32, 12);
            Osc("Sport", "Fait du vélo sur le dos", "0 10 25 5 28 -5 110 -100 60 -20 0 -75", "0 10 25 5 28 -5 60 -20 110 -100 0 -75", 0.5, 8);
            Osc("Sport", "Moulin à vent, mains aux pieds", "70 20 160 0 20 0 30 0 -30 0", "70 20 20 0 160 0 30 0 -30 0", 0.9, 6);
            Osc("Sport", "Étire ses quadriceps", "0 0 -30 -20 60 20 6 0 -20 -130", "2 0 -32 -22 62 22 6 0 -24 -134", 1.4, 3);
            Cles("Sport", "Départ de sprint", "0:" + S + ";400:60 30 30 0 20 0 80 -110 30 -80;1100:60 30 30 0 20 0 80 -110 30 -80;1300:40 10 -40 90 80 80 90 -60 -30 -20 0 0 10;1600:" + S);
            Osc("Sport", "Lancers francs", "0 -6 150 60 140 70 12 -20 -10 -20", "-4 -12 170 10 165 15 6 0 -6 0 8", 1.1, 4, "ballontir");

            // --- quotidien
            Osc("Quotidien", "Se brosse les dents", "0 4 95 150 -8 12 6 0 -6 0", "0 6 99 146 -8 12 6 0 -6 0", 0.2, 14);
            Osc("Quotidien", "Passe l'aspirateur", "16 10 60 20 50 30 20 -16 -16 -10", "10 8 80 10 70 20 14 -10 -20 -14", 0.9, 5);
            Osc("Quotidien", "Repasse une chemise", "20 12 70 10 30 60 10 -8 -10 -6", "20 12 50 30 30 60 10 -8 -10 -6", 0.7, 6);
            Osc("Quotidien", "Fait la vaisselle", "14 14 55 60 50 65 8 -4 -8 -4", "14 16 60 54 46 71 8 -4 -8 -4", 0.4, 8);
            Osc("Quotidien", "Attend le bus", "-2 0 10 12 -10 12 6 0 -6 0", "-2 14 60 110 -10 12 8 -4 -4 0", 2.4, 2);
            Osc("Quotidien", "Fait des pompes de canard", "30 10 -30 -100 -35 -105 80 -120 70 -112", "34 12 -20 -110 -25 -115 84 -124 74 -116", 0.4, 8);
            Cles("Quotidien", "S'étire au réveil", "0:" + S + ";500:-10 -20 170 10 -170 -10 6 0 -6 0 4;1300:-14 -26 176 -6 -176 6 6 0 -6 0 6;1900:6 10 30 40 -30 40 6 0 -6 0;2300:" + S);

            // --- émotions
            Cles("Émotions", "Explose de rire, plié en deux", "0:" + S + ";250:40 30 40 100 45 105 20 -30 -10 -20;450:46 34 44 104 49 109 24 -36 -12 -24;650:40 30 40 100 45 105 20 -30 -10 -20;850:46 34 44 104 49 109 24 -36 -12 -24;1200:-10 -20 30 60 -30 60 6 0 -6 0;1600:" + S);
            Osc("Émotions", "Boude, bras croisés", "-6 16 40 120 45 115 6 0 -6 0", "-8 20 42 122 47 117 6 0 -6 0", 1.6, 3);
            Cles("Émotions", "Sursaute de peur", "0:" + S + ";120:-20 -20 120 40 -120 -40 20 -30 -20 -30 14;400:-14 -10 100 60 -100 -60 14 -20 -14 -20;900:" + S, "exclam");
            Osc("Émotions", "Rêvasse, la tête dans les nuages", "-4 -24 -30 -100 -35 -105 8 0 -8 0", "-6 -30 -32 -102 -37 -107 8 -4 -4 0", 2.6, 3, "note");
            Cles("Émotions", "Danse de la joie", "0:" + S + ";200:-6 -10 150 20 40 100 30 -50 -10 0 10;400:6 -10 40 100 150 20 -10 0 30 -50 10;600:-6 -10 150 20 40 100 30 -50 -10 0 10;800:6 -10 40 100 150 20 -10 0 30 -50 10;1000:" + S);
            Osc("Émotions", "Hésite, se balance d'un pied sur l'autre", "-4 6 20 100 -10 12 10 -10 -4 0", "4 6 20 100 -10 12 4 0 -10 -10", 0.9, 5, "question");
            Cles("Émotions", "Fond en larmes", "0:" + S + ";400:20 30 130 135 -130 -135 10 -8 -10 -8;700:24 34 134 139 -134 -139 12 -12 -12 -12;1000:20 30 130 135 -130 -135 10 -8 -10 -8;1300:24 34 134 139 -134 -139 12 -12 -12 -12;1800:" + S);
        }

        // ------------------------------------------------------------ cinquième fournée

        static void Cinquieme()
        {
            // --- déplacements
            Pas("Sprint à fond", 55, 110, 60, 95, 24, 8, 340, 0.36);
            Pas("Marche décontractée", 22, 34, 34, 10, -4, 0, 60, 1.05);
            Pas("Marche pressée, penché en avant", 30, 48, 30, 40, 14, 0, 110, 0.7);
            Pas("Trottine sur la pointe", 20, 50, 14, 70, 2, 6, 90, 0.4);
            Pas("Grandes enjambées", 48, 60, 36, 10, 6, 2, 130, 0.95);
            Pas("Pas de parade", 40, 10, 45, 0, 0, 0, 80, 0.8);
            Pas("Jogging tranquille", 36, 80, 34, 90, 8, 5, 170, 0.52);
            Pas("Marche lourde", 24, 44, 10, 10, 10, 3, 45, 1.2);
            Pas("Course paniquée", 50, 100, 70, 40, 20, 6, 300, 0.34);

            // --- gestes
            Osc("Gestes", "V de la victoire", "0 -6 150 10 -8 12", "0 -8 155 18 -8 12", 0.6, 4);
            Osc("Gestes", "Se gratte le menton", "4 10 70 140 -8 12", "4 12 72 144 -8 12", 0.5, 5);
            Osc("Gestes", "Claque des doigts", "0 0 95 60 -8 12", "0 2 95 72 -8 12", 0.35, 6);
            Osc("Gestes", "Fait signe d'approcher", "0 0 90 20 -8 12", "0 0 90 110 -8 12", 0.5, 5);
            Osc("Gestes", "Fait signe de partir", "0 0 100 60 -8 12", "0 0 70 10 -8 12", 0.5, 5);
            Osc("Gestes", "Demande la parole", "0 -4 172 0 -8 12", "0 -6 176 6 -8 12", 0.8, 3);
            Osc("Gestes", "Se tape le front", "0 6 110 150 -8 12", "0 10 100 140 -8 12", 0.5, 3);
            Osc("Gestes", "Se recoiffe", "0 8 140 130 -8 12", "0 4 150 120 -8 12", 0.5, 5);
            Osc("Gestes", "S'évente de la main", "0 -4 85 120 -8 12", "0 -4 85 150 -8 12", 0.25, 10);
            Osc("Gestes", "Compte sur ses doigts", "6 14 60 110 50 120", "6 16 62 114 54 112", 0.45, 6);
            Osc("Gestes", "Applaudit lentement", "0 0 60 75 60 75", "0 0 48 95 48 95", 0.7, 5);
            Osc("Gestes", "Tape du poing dans sa main", "6 6 70 70 55 90", "6 8 58 92 55 90", 0.4, 6);

            // --- combat
            Cles("Combat", "Uppercut sauté", "0:" + G + ";180:" + Cr + ";360:-6 -12 175 5 55 110 20 -30 -10 -40 38;560:-4 -8 160 20 55 110 30 -50 0 -40 20;760:" + Cr + ";950:" + G);
            Cles("Combat", "Balayette", "0:" + G + ";200:" + Cr + ";420:30 10 40 60 -60 20 100 -150 80 -10 0 0 6;600:30 10 40 60 -60 20 100 -150 60 -10 0 0 6;800:" + Cr + ";1000:" + G);
            Osc("Combat", "Double coup de poing", "8 0 60 110 55 115 14 -15 -14 -10", "18 0 92 0 88 0 22 -22 -20 -6 0 0 6", 0.45, 5);
            Osc("Combat", "Garde basse, feintes", "14 4 40 80 30 90 18 -22 -16 -14", "6 0 50 70 40 80 12 -16 -20 -18 0 0 5", 0.4, 8);
            Cles("Combat", "Parade haute puis basse", "0:" + G + ";200:-4 -6 140 90 130 100 14 -15 -14 -10;450:-4 -6 140 90 130 100 14 -15 -14 -10;650:16 8 30 60 20 70 20 -26 -16 -14;900:16 8 30 60 20 70 20 -26 -16 -14;1100:" + G);

            // --- acrobaties
            Fixe("Acrobaties", "Équerre", "0 -5 20 0 25 0 95 0 90 0 14", 3);
            Cles("Acrobaties", "Saut écart", "0:" + S + ";180:" + Cr + ";420:0 -10 100 0 -100 0 80 0 -80 0 46;600:0 -10 100 0 -100 0 80 0 -80 0 40;800:" + Cr + ";1000:" + S);
            Cles("Acrobaties", "Saut carpé jambes écartées", "0:" + S + ";180:" + Cr + ";420:40 10 80 0 70 0 110 0 60 0 50;620:" + Cr + ";820:" + S);
            Cles("Acrobaties", "Pont arrière", "0:" + S + ";400:-40 -30 170 0 -175 0 10 -20 -5 -25;900:-95 -40 185 0 190 0 30 -70 20 -65 6;1500:-95 -40 185 0 190 0 30 -70 20 -65 6;1900:-40 -30 170 0 -175 0 10 -20 -5 -25;2300:" + S);

            // --- sport
            Cles("Sport", "Squats sautés", "0:" + S + ";250:" + Cr + ";450:-2 -8 160 5 -160 -5 6 0 -6 0 34;650:" + Cr + ";900:" + Cr + ";1100:-2 -8 160 5 -160 -5 6 0 -6 0 40;1300:" + Cr + ";1550:" + S);
            Osc("Sport", "Fentes latérales", "8 0 60 80 55 85 55 -70 -40 0", "8 0 60 80 55 85 40 0 -55 -70", 1.2, 5);
            Osc("Sport", "Montées de genoux", "0 0 -30 90 40 90 95 -100 0 -5", "0 0 40 90 -30 90 0 -5 95 -100", 0.4, 10);
            Osc("Sport", "Talons-fesses", "6 0 30 90 -20 90 10 -130 -6 -5", "6 0 -20 90 30 90 -6 -5 10 -130", 0.36, 10);
            Fixe("Sport", "Gainage sur un bras", "0 -10 62 0 242 0 0 0 2 0 0 62", 3);
            Osc("Sport", "Boxe à vide", "10 0 92 0 55 115 16 -18 -18 -10", "10 0 60 110 88 0 16 -18 -18 -10", 0.3, 12);
            Osc("Sport", "Pompes sur un bras", "0 -10 62 0 -20 100 0 0 2 0 0 62", "0 -8 100 -80 -20 100 0 0 2 0 0 72", 1.1, 5);

            // --- quotidien
            Osc("Quotidien", "Frissonne de froid", "8 10 40 130 45 125 8 -6 -8 -4", "9 11 43 127 42 128 9 -7 -7 -5", 0.09, 30);
            Fixe("Quotidien", "Fait la statue", "-4 -10 120 60 -60 -30 20 -30 -10 0", 4);
            Osc("Quotidien", "Fait les poussières", "6 -10 150 20 -10 12 8 0 -8 0", "6 -14 165 -10 -10 12 8 0 -8 0", 0.4, 8);
            Osc("Quotidien", "Porte un carton lourd", "-8 0 50 60 45 65 14 -20 -10 -16", "-10 2 52 62 47 67 16 -24 -12 -18", 0.9, 4);
            Osc("Quotidien", "Se chauffe les mains", "10 10 70 40 65 45 10 -10 -10 -8", "10 12 72 46 63 39 10 -10 -10 -8", 0.5, 6);

            // --- émotions
            Cles("Émotions", "Saute de joie, bras en V", "0:" + S + ";160:" + Cr + ";380:-4 -14 155 0 -155 0 10 -10 -10 -10 42;560:" + Cr + ";740:-4 -14 155 0 -155 0 10 -10 -10 -10 48;920:" + Cr + ";1100:" + S);
            Osc("Émotions", "Se prend la tête à deux mains", "10 20 140 130 -140 -130 8 -4 -8 -4", "14 26 144 134 -144 -134 10 -8 -10 -8", 0.9, 4);
            Osc("Émotions", "Timide, se tortille", "-4 14 -20 -30 -25 -35 6 0 -4 -14", "4 18 -22 -32 -27 -37 4 -14 -6 0", 0.8, 5);
            Osc("Émotions", "Tremble de peur", "-14 -6 60 120 50 130 12 -20 -6 -14", "-15 -5 62 122 52 128 13 -22 -5 -16", 0.08, 34);
            Osc("Émotions", "Applaudit en sautillant", "0 -6 70 70 70 70 8 -10 -8 -10 6", "0 -8 55 95 55 95 6 0 -6 0", 0.3, 10);
            Cles("Émotions", "Soupir de soulagement", "0:" + S + ";400:-8 -16 20 20 -20 20 6 0 -6 0;900:10 18 10 10 -10 10 8 -6 -8 -6;1500:10 18 10 10 -10 10 8 -6 -8 -6;1900:" + S);
        }

        // ------------------------------------------------------------ quatrième fournée

        static void Quatrieme()
        {
            // --- gestes
            Osc("Gestes", "Coucou des deux mains", "0 -5 150 30 -150 -30", "0 -5 150 80 -150 -80", 0.4, 6);
            Osc("Gestes", "Jumelles", "6 -8 110 140 100 150", "8 -4 112 142 102 152", 1.4, 3);
            Osc("Gestes", "Chut !", "4 0 95 150 -8 12", "4 2 97 152 -8 12", 1.0, 3);
            Osc("Gestes", "Montre ses biceps", "-4 -8 100 110 -100 -110", "-4 -10 108 125 -108 -125", 0.6, 5);
            Osc("Gestes", "Pouce en l'air", "0 -6 95 20 -8 12", "0 -6 100 28 -8 12", 0.5, 5);
            Osc("Gestes", "Non du doigt", "0 0 80 80 -8 12", "0 0 80 110 -8 12", 0.3, 8);
            Osc("Gestes", "Mains en porte-voix", "6 -12 105 150 95 155", "8 -14 107 152 97 157", 0.8, 4);
            Osc("Gestes", "Hausse les épaules deux fois", "0 0 30 100 -30 -100", "0 -8 55 95 -55 -95", 0.5, 4);

            // --- corps entier
            Osc("Quotidien", "Bras croisés, tape du pied", "-4 0 40 120 45 115 6 0 -6 0", "-4 0 40 120 45 115 6 0 10 -12", 0.4, 8);
            Osc("Quotidien", "Tape sur un clavier debout", "10 12 60 80 55 85 6 0 -6 0", "10 14 64 76 51 89 6 0 -6 0", 0.16, 16);
            Osc("Quotidien", "Arrose des fleurs", "8 10 70 30 -10 12 8 -4 -8 -2", "10 12 60 45 -10 12 8 -4 -8 -2", 1.0, 4);
            Cles("Émotions", "Révérence profonde", "0:" + S + ";400:55 30 -40 -10 100 20 20 -10 -20 -5;1100:55 30 -40 -10 100 20 20 -10 -20 -5;1500:" + S);
            Cles("Émotions", "Tombe à la renverse de surprise", "0:" + S + ";150:-20 -25 120 30 -120 -30 10 -5 -10 -5;400:" + Dos + ";1400:" + Dos + ";1800:" + Cr + ";2100:" + S, "exclam");
            Osc("Émotions", "Trépigne d'impatience", "0 0 30 100 -30 -100 6 0 20 -45", "0 0 30 100 -30 -100 20 -45 -6 0", 0.25, 10);
            Osc("Émotions", "Fier comme un coq", "-10 -15 -30 -100 -35 -105 8 0 -8 0", "-12 -18 -32 -102 -37 -107 8 0 -8 0", 1.2, 3);
            Osc("Sport", "Touche ses orteils", "0 0 170 5 -170 -5 4 0 -4 0", "95 30 20 0 25 0 4 0 -4 0", 1.6, 4);
            Osc("Sport", "Chaise invisible", "0 0 90 0 85 0 90 -90 84 -88", "2 0 92 0 87 0 92 -92 86 -90", 0.8, 6);
            Osc("Sport", "Étirement latéral", "-25 -10 170 -30 -20 -20 10 0 -10 0", "25 10 -170 30 20 20 10 0 -10 0", 2.0, 3);
            Ajouter("Sport", "Grands moulinets des bras", 1.0, 5, t =>
            {
                double[] p = L(S);
                p[I.Ep1] = 360 * t; p[I.Co1] = 0; p[I.Ep2] = 360 * t + 180; p[I.Co2] = 0;
                return p;
            });
            Cles("Acrobaties", "Bonds de grenouille, de plus en plus haut", "0:" + Cr + ";180:30 -20 60 20 50 25 100 -140 95 -135;400:0 -10 160 10 150 15 20 -10 -20 -10 55;620:" + Cr
                + ";800:30 -20 60 20 50 25 100 -140 95 -135;1020:0 -10 160 10 150 15 20 -10 -20 -10 70;1240:" + Cr + ";1500:" + S);
            Osc("Danses", "Twist des genoux", "0 0 60 90 -60 -90 20 -40 -5 -40", "0 0 -60 -90 60 90 -5 -40 20 -40", 0.6, 8);
            Osc("Danses", "Arroseur automatique", "-6 -10 100 150 90 0 14 -20 -8 -14", "-6 10 100 150 40 0 -8 -14 14 -20", 0.5, 8);
        }

        // ------------------------------------------------------------ à deux
        // Les distances sont calculées pour que les mains se rejoignent à mi-chemin (bras de 19 + 19).

        static void ADeux()
        {
            const string bras2 = " -15 20 ", debout = " 8 -4 -8 -2";

            // bonjour : chacun fait signe de la main
            const string signe1 = "-2 -5 155 -30 -10 12 6 0 -6 0", signe2 = "-2 -5 160 25 -10 12 6 0 -6 0";
            Deux("Bonjour", 95, "Salut !", "Hé, salut !", null, null,
                "0:" + S + ";250:" + signe1 + ";450:" + signe2 + ";650:" + signe1 + ";850:" + signe2 + ";1050:" + signe1 + ";1400:" + S,
                "0:" + S + ";200:" + S + ";450:" + signe2 + ";650:" + signe1 + ";850:" + signe2 + ";1050:" + signe1 + ";1250:" + signe2 + ";1400:" + S, null);

            // check : poing contre poing
            const string armeP = "4 0 40 110" + bras2 + "12 -8 -10 -4", poing = "8 0 90 0 -20 25 16 -12 -14 -4";
            string check = "0:" + S + ";300:" + armeP + ";480:" + poing + ";700:" + poing + ";900:0 -6 60 90" + bras2 + debout + ";1200:" + S;
            Deux("Check", 86, "Check !", "Check !", null, null, check, check, "coup@0.4");

            // tope-là : les mains claquent en l'air
            const string armeT = "-4 -8 170 20" + bras2 + debout, tope = "4 -5 137 0 -20 25 12 -8 -10 -4";
            string topeLa = "0:" + S + ";280:" + armeT + ";430:" + tope + ";600:" + tope + ";850:0 0 100 30 -10 12 6 0 -6 0;1100:" + S;
            Deux("Tope-là", 60, "Tope là !", "Yeah !", null, null, topeLa, topeLa, "pop@0.39");

            // poignée de main : trois secousses
            const string tend = "6 4 60 20 -12 14 10 -4 -8 -2", secoueH = "6 4 64 34 -12 14 10 -4 -8 -2", secoueB = "6 4 56 8 -12 14 10 -4 -8 -2";
            string serre = "0:" + S + ";350:" + tend + ";500:" + secoueH + ";650:" + secoueB + ";800:" + secoueH + ";950:" + secoueB + ";1100:" + tend + ";1400:" + S;
            Deux("Poignée de main", 76, "Enchanté !", "Ça va ?", null, null, serre, serre, null);

            // check complet : en haut, en bas, puis le poing
            const string haut = "2 -4 145 -35" + bras2 + debout, bas = "2 6 40 30" + bras2 + "10 -6 -8 -2", cogne = "2 0 125 -70" + bras2 + "10 -6 -8 -2";
            string complet = "0:" + S + ";250:0 -5 165 10" + bras2 + debout + ";400:" + haut + ";520:" + haut + ";700:" + bas + ";820:" + bas
                + ";1000:2 0 60 100" + bras2 + debout + ";1150:" + cogne + ";1300:" + cogne + ";1500:-8 -8 110 60 -40 40 6 0 -8 -4;1800:" + S;
            Deux("Check complet", 62, "Yo !", "Yo !", null, "Trop stylé !", complet, complet, "pop@0.22 pop@0.39 coup@0.64");

            // câlin
            const string ouvre = "-4 -6 100 10 -100 -10 8 -2 -8 -2", serre1 = "10 12 75 60 85 40 12 -6 -6 -2", serre2 = "12 14 78 64 88 44 12 -6 -6 -2";
            string calin = "0:" + S + ";300:" + ouvre + ";600:" + serre1 + ";900:" + serre2 + ";1200:" + serre1 + ";1500:" + serre2 + ";1800:" + ouvre + ";2100:" + S;
            Deux("Câlin", 46, "Câlin !", "♥", null, null, calin, calin, null);

            // pierre-feuille-ciseaux : trois secousses du poing, on montre, l'un gagne et l'autre non
            const string pfcH = "4 6 55 60 -12 14" + debout, pfcB = "4 8 45 45 -12 14" + debout, montre = "6 4 70 15 -12 14 10 -4 -8 -2";
            string compte = "0:" + S + ";250:" + pfcH + ";400:" + pfcB + ";550:" + pfcH + ";700:" + pfcB + ";850:" + pfcH + ";1000:" + pfcB + ";1150:" + montre + ";1500:" + montre;
            Duo pfc = Deux("Pierre-feuille-ciseaux", 96, "Pierre, feuille, ciseaux !", "…ciseaux !", "Gagné !", "Oh non…",
                compte + ";1800:-6 -12 165 10 -165 -10 8 0 -8 0 10;2100:-6 -14 160 -10 -160 10 8 -10 -8 -10;2400:-6 -12 165 10 -165 -10 8 0 -8 0 8;2700:" + S,
                compte + ";1800:14 28 20 150 -10 12 6 0 -6 0;2400:16 32 22 152 -10 12 6 -4 -6 -4;2700:" + S, null);
            pfc.Hasard = true;

            // duel amical : un coup de poing esquivé, un coup de pied paré, puis on se salue
            const string pare = "6 4 40 50 30 60 14 -15 -14 -10", salue = "22 10 5 5 -5 5 4 0 -4 0";
            Deux("Duel amical", 74, "En garde !", "Viens !", "Bien joué !", "Bien joué !",
                "0:" + S + ";300:" + G + ";450:16 0 90 0 50 115 24 -22 -22 -6 0 0 8;650:" + G + ";1000:" + pare + ";1250:" + pare + ";1500:" + G + ";1800:" + S + ";2100:" + salue + ";2500:" + S,
                "0:" + S + ";300:" + G + ";450:-22 -10 60 95 50 105 22 -18 -6 -6 0 0 -8;650:" + G + ";850:-6 0 70 100 55 110 60 -90 -10 -12;1000:-12 0 70 100 55 110 95 -5 -12 -14 0 0 4;1250:" + G + ";1500:" + G + ";1800:" + S + ";2100:" + salue + ";2500:" + S,
                "swish@0.18 coup@0.4");

            // coude contre coude
            const string coude = "6 0 80 120" + bras2 + "12 -8 -10 -4";
            string coudes = "0:" + S + ";300:0 0 30 130" + bras2 + debout + ";480:" + coude + ";700:" + coude + ";950:" + S;
            Deux("Coude contre coude", 50, "Coude !", "Coude !", null, null, coudes, coudes, "coup@0.5");

            // double tope : les deux mains, deux fois
            const string armes2 = "-4 -8 170 20 160 25" + debout, topes2 = "4 -5 137 0 131 4 12 -8 -10 -4", entre2 = "0 -4 110 40 100 45" + debout;
            string double2 = "0:" + S + ";250:" + armes2 + ";400:" + topes2 + ";520:" + entre2 + ";660:" + topes2 + ";820:" + topes2 + ";1050:" + S;
            Deux("Double tope", 60, "Des deux mains !", "Allez !", null, "Encore !", double2, double2, "pop@0.38 pop@0.63");

            // révérence : chacun s'incline, très poli
            const string incline = "35 25 -30 -10 100 20 14 -8 -14 -4";
            string reverence = "0:" + S + ";400:" + incline + ";1000:" + incline + ";1400:" + S;
            Deux("Révérence", 110, "Après vous.", "Mais non, après vous !", null, null, reverence, reverence, null);

            // tchin : les verres se cognent, puis chacun boit
            const string leveV = "4 -4 105 0" + bras2 + "10 -6 -8 -2", boitV = "-6 -16 70 110" + bras2 + debout;
            string tchin = "0:" + S + ";300:0 0 60 60" + bras2 + debout + ";500:" + leveV + ";700:" + leveV + ";1000:" + boitV + ";1600:" + boitV + ";1900:" + S;
            Deux("Tchin !", 80, "Santé !", "Tchin !", null, "Ahh…", tchin, tchin, "note@0.27");

            // check du pied : les semelles se touchent deux fois
            const string pied = "-6 0 20 20 -30 30 55 -5 -4 -4", repli = "-2 0 20 20 -30 30 40 -70 -4 -4";
            string duPied = "0:" + S + ";250:" + repli + ";400:" + pied + ";520:" + repli + ";660:" + pied + ";800:" + repli + ";1050:" + S;
            Deux("Check du pied", 70, "Du pied !", "Du pied !", null, null, duPied, duPied, "coup@0.38 coup@0.63");

            // garde-à-vous : chacun salue, raide comme un piquet
            const string fixe = "-2 -4 6 6 -6 6 2 0 -2 0", saluer = "-2 -4 110 100 -6 6 2 0 -2 0";
            string garde = "0:" + S + ";250:" + fixe + ";500:" + saluer + ";1300:" + saluer + ";1550:" + fixe + ";1800:" + S;
            Deux("Garde-à-vous", 100, "Garde à vous !", "Chef, oui chef !", "Repos.", null, garde, garde, null);

            // la ola : l'un lève les bras, l'autre suit, et ça repart
            const string olaH = "-6 -10 165 10 -165 -10 8 0 -8 0 6", olaB = "14 10 30 10 -30 -10 30 -50 -20 -40";
            Deux("La ola", 110, "Olaaa !", "Olaaa !", null, null,
                "0:" + S + ";250:" + olaB + ";500:" + olaH + ";800:" + olaB + ";1100:" + olaH + ";1400:" + olaB + ";1700:" + S,
                "0:" + S + ";450:" + olaB + ";700:" + olaH + ";1000:" + olaB + ";1300:" + olaH + ";1550:" + olaB + ";1700:" + S, null);

            // « trop lent ! » : il tend la main pour un tope-là, puis la retire au dernier moment
            const string lisse = "-4 -6 160 130" + bras2 + debout;      // la main passe dans les cheveux
            Deux("Trop lent !", 60, "Tope là !", "Yeah !", "Trop lent !", "Hé !",
                "0:" + S + ";280:" + armeT + ";400:" + armeT + ";520:" + lisse + ";1100:-4 -6 150 140" + bras2 + debout + ";1500:" + S,
                "0:" + S + ";280:" + armeT + ";430:" + tope + ";560:20 14 100 10 -20 25 20 -16 -14 -6 0 0 6;900:14 20 20 10 -15 20 10 -6 -8 -4;1500:" + S, "swish@0.3");

            // concours de pompes : face à face, et c'est le hasard qui désigne le vainqueur
            const string pompeB = "0 -10 25 135 27 135 0 0 2 0 0 80";
            string pompes = "0:" + S + ";350:" + Cr + ";650:" + Planche + ";900:" + pompeB + ";1150:" + Planche + ";1400:" + pompeB + ";1650:" + Planche + ";1900:" + pompeB + ";2150:" + Planche;
            Duo concours = Deux("Concours de pompes", 150, "Concours de pompes !", "Tu vas perdre !", "Gagné !", "Je suis mort…",
                pompes + ";2400:" + pompeB + ";2650:" + Planche + ";2950:" + Cr + ";3200:-6 -12 165 10 -165 -10 8 0 -8 0 10;3500:" + S,
                pompes + ";2450:" + pompeB + ";2800:" + Ventre + ";3200:" + Ventre + ";3350:" + Cr + ";3500:" + S, null);
            concours.Hasard = true;

            // applaudissements : ils se félicitent
            const string clap1 = "2 0 45 75 78 45 6 0 -6 0", clap2 = "2 0 60 62 62 58 6 0 -6 0";
            string bravo = "0:" + S + ";250:" + clap1 + ";400:" + clap2 + ";550:" + clap1 + ";700:" + clap2 + ";850:" + clap1 + ";1000:" + clap2 + ";1150:" + clap1 + ";1300:" + clap2 + ";1600:" + S;
            Deux("Applaudissements", 90, "Bravo !", "Bravo à toi !", null, null, bravo, bravo, "pop@0.25 pop@0.44 pop@0.63 pop@0.81");

            // bras de fer debout : paume contre paume, on pousse, et le hasard désigne le plus fort
            const string pousse = "20 0 85 0 80 4 30 -30 -30 -6", pousseFort = "28 2 88 0 84 2 40 -40 -34 -4";
            string lutte = "0:" + S + ";350:" + pousse + ";700:" + pousseFort + ";1000:" + pousse + ";1300:" + pousseFort + ";1600:" + pousse;
            Duo fer = Deux("Bras de fer", 100, "On pousse !", "Tu ne passeras pas !", "Trop fort !", "Hé, doucement !",
                lutte + ";1900:" + pousseFort + " 0 0 12;2300:-6 -12 165 10 -165 -10 8 0 -8 0 8;2700:" + S,
                lutte + ";1900:-24 -10 100 30 90 40 -20 -10 20 -30 0 0 -14;2300:-10 0 60 40 50 50 -10 -10 14 -20 0 0 -8;2700:" + S, null);
            fer.Hasard = true;

            // coup d'épaule sauté : ils se cognent en l'air, comme après un but
            string epaule = "0:" + S + ";250:" + Cr + ";450:-10 0 -30 20 -40 20 20 -30 -10 -30 26 0 14;650:" + Cr + ";900:" + S;
            Deux("Coup d'épaule sauté", 60, "Ouais !", "Ouais !", null, null, epaule, epaule, "coup@0.5");

            // tape-mains : dans ses mains, puis dans celle de l'autre, une main après l'autre
            const string paume1 = "4 -5 137 0 -20 25 12 -8 -10 -4", paume2 = "4 -5 -20 25 137 0 12 -8 -10 -4";
            string tapes = "0:" + S + ";250:" + clap2 + ";400:" + paume1 + ";550:" + clap2 + ";700:" + paume2 + ";850:" + clap2 + ";1000:" + paume1 + ";1150:" + clap2 + ";1300:" + paume2 + ";1600:" + S;
            Deux("Tape-mains", 60, "Un, deux, trois…", "…soleil !", null, "Encore !", tapes, tapes, "pop@0.25 pop@0.44 pop@0.63 pop@0.81");

            // dispute et réconciliation : ils s'accusent du doigt, boudent, puis finissent dans les bras l'un de l'autre
            const string accuse1 = "10 -6 95 0 -30 100 8 -4 -8 -2 0 0 -20", accuse2 = "14 -4 100 20 -30 100 8 -4 -8 -2 0 0 -20", boudeur = "-4 14 40 110 30 120 6 0 -6 0 0 0 -20";
            string dispute = "0:" + S + ";300:" + accuse1 + ";500:" + accuse2 + ";700:" + accuse1 + ";900:" + accuse2 + ";1200:" + boudeur + ";1900:" + boudeur
                + ";2200:" + ouvre + ";2500:" + serre1 + ";2900:" + serre2 + ";3200:" + ouvre + ";3500:" + S;
            Deux("Dispute et réconciliation", 46, "C'est ta faute !", "Non, la tienne !", "Pardon…", "Moi aussi…", dispute, dispute, null);

            // danse à deux : la même danse, face à face (sur le tempo s'il y a de la musique)
            Duos.Add(new Duo { Nom = "Danse à deux", Distance = 85, DitA = "On danse ?", DitB = "Carrément !" });
        }

        static Duo Deux(string nom, double distance, string ditA, string ditB, string finA, string finB, string a, string b, string bruits)
        {
            var d = new Duo { Nom = nom, Distance = distance, DitA = ditA, DitB = ditB, FinA = finA, FinB = finB, A = Cles(null, nom, a), B = Cles(null, nom, b) };
            if (bruits != null)
            {
                string[] morceaux = bruits.Split(' ');
                d.A.Bruits = new string[morceaux.Length];
                d.A.BruitsA = new double[morceaux.Length];
                for (int i = 0; i < morceaux.Length; i++)
                {
                    string[] m = morceaux[i].Split('@');
                    d.A.Bruits[i] = m[0];
                    d.A.BruitsA[i] = double.Parse(m[1], CultureInfo.InvariantCulture);
                }
            }
            Duos.Add(d);
            return d;
        }

        // ------------------------------------------------------------ troisième fournée

        static void Troisieme()
        {
            // --- déplacements
            Pas("Course au ralenti", 45, 95, 50, 90, 16, 5, 60, 1.7);
            Pas("Titube", 16, 20, 0, 0, 6, 0, 30, 1.5, 7);
            Pas("Course en arrière", 38, 70, 40, 85, -8, 4, -150, 0.5);
            Pas("Shuffle", 20, 45, 25, 80, 4, 4, 100, 0.35);
            Anim cloche = Ajouter("Déplacements", "À cloche-pied", 0.5, 1, t =>
            {
                double u = Math.Max(0, Math.Sin(2 * Math.PI * t));
                double[] p = L("4 0 40 80 -40 80 4 -30 30 -100");
                p[I.Air] = 16 * u; p[I.Ge1] = -30 + 26 * u; p[I.Ha1] = 12 - 8 * u;
                return p;
            });
            cloche.Deplace = true; cloche.Vitesse = 70;
            Anim genoux = Ajouter("Déplacements", "Marche sur les genoux", 0.9, 1, t =>
            {
                double s = Math.Sin(2 * Math.PI * t);
                double[] p = L("0 0 0 20 0 20 0 -95 0 -95");
                p[I.Ha1] = 16 * s; p[I.Ha2] = -16 * s; p[I.Ep1] = -20 * s; p[I.Ep2] = 20 * s; p[I.Torse] = 3 * s;
                return p;
            });
            genoux.Deplace = true; genoux.Vitesse = 26;
            Anim boite = Ajouter("Déplacements", "Boitille", 1.1, 1, t =>
            {
                double f = 2 * Math.PI * t, s = Math.Sin(f), c = Math.Cos(f);
                double[] p = L("8 5 0 20 0 20 0 0 0 0");
                p[I.Ha1] = 24 * s; p[I.Ge1] = -40 * Math.Max(0, c); p[I.Ha2] = -10 * s; p[I.Torse] = 8 + 9 * Math.Max(0, -s); p[I.Ep1] = -14 * s; p[I.Ep2] = 30 + 10 * s; p[I.Co2] = 70;
                return p;
            });
            boite.Deplace = true; boite.Vitesse = 30;
            Anim ours = Ajouter("Déplacements", "À quatre pattes", 0.8, 1, t =>
            {
                double s = Math.Sin(2 * Math.PI * t);
                double[] p = L("0 -30 0 0 0 0 0 0 0 0 0 55");
                p[I.Ep1] = 55 + 22 * s; p[I.Ep2] = 55 - 22 * s; p[I.Ha1] = 40 - 24 * s; p[I.Ge1] = -40 - 20 * Math.Max(0, s); p[I.Ha2] = 40 + 24 * s; p[I.Ge2] = -40 - 20 * Math.Max(0, -s);
                return p;
            });
            ours.Deplace = true; ours.Vitesse = 60;
            Anim plane = Ajouter("Déplacements", "Vole comme un super-héros", 1.2, 1, t =>
            {
                double s = Math.Sin(2 * Math.PI * t);
                double[] p = L("0 -30 172 0 10 5 -8 0 -4 -10 70 80");
                p[I.Air] = 70 + 6 * s; p[I.Rot] = 80 + 3 * s; p[I.Ha2] = -4 + 5 * s;
                return p;
            });
            plane.Deplace = true; plane.Vitesse = 210; plane.Son = "lance";

            // --- combat
            const string F = "Combat";
            Coup("Coup de tête", "-15 -20 60 100 50 105 10 -15 -16 -10", "35 25 40 60 30 70 30 -30 -24 -8 0 0 10");
            Cles(F, "Coup de pied en vrille", "0:" + G + ";140:-6 0 60 95 45 105 30 -40 -8 -12 6 90;260:-20 0 55 100 30 110 100 -4 -10 -10 32 200;380:-10 0 60 95 45 105 60 -60 -8 -12 14 320;500:" + G + " 0 360");
            Cles(F, "Coup de pied en ciseaux", "0:" + G + ";140:" + Cr + ";280:-15 0 60 95 45 105 110 -4 -30 -70 40;400:-15 0 60 95 45 105 -20 -70 115 -4 46;540:-5 0 60 95 45 105 40 -50 20 -45 18;660:" + Cr + ";820:" + G);
            double[] g = L(G), pied = L("-16 0 55 100 30 110 92 -4 -8 -10"), piedHaut = L("-30 -5 40 100 20 110 132 -4 -10 -8"), piedBas = L("-4 0 60 95 45 105 52 -2 -8 -12");
            ClesP(F, "Pluie de coups de pied", new[] { 0, 0.14, 0.26, 0.38, 0.5, 0.62, 0.74, 0.86, 1.1 }, new[] { g, piedBas, g, pied, g, piedHaut, pied, piedHaut, g });
            Cles(F, "Poing du dragon", "0:" + G + ";180:22 5 10 95 55 110 62 -112 52 -104;340:-8 -15 150 30 50 115 20 -30 -10 -60 55;500:-10 -18 170 10 50 115 10 -10 -14 -40 82;680:0 0 100 40 50 100 30 -50 20 -45 30;800:" + Cr + ";1000:" + G);
            Cles(F, "Marteau géant", "0:" + G + ";300:-15 -10 170 10 175 5 14 -10 -16 -6;430:45 15 40 0 45 0 60 -95 30 -70;900:45 15 40 0 45 0 60 -95 30 -70;1200:" + G, "marteau");
            Cles(F, "Lancer de javelot", "0:" + S + ";300:-15 -5 -110 20 70 10 -20 -10 22 -15;430:20 5 100 0 -40 30 28 -26 -24 -6 0 0 10;800:22 6 60 5 -40 30 28 -26 -24 -6 0 0 10;1100:" + S, "javelot");
            Ajouter(F, "Nunchaku", 0.6, 6, t =>
            {
                double f = 2 * Math.PI * t;
                double[] p = L(G);
                p[I.Ep1] = 60 + 50 * Math.Sin(f); p[I.Co1] = 60 + 40 * Math.Cos(2 * f);
                return p;
            }).Objet = "baton";
            Osc(F, "Esquives de boxeur", "20 5 75 100 60 108 30 -40 -6 -30 0 0 -8", "20 5 75 100 60 108 10 -35 -26 -40 0 0 8", 0.5, 6);
            Cles(F, "Épée : parade et riposte", "0:6 0 40 80 -20 60 14 -15 -14 -10;200:0 0 70 110 -20 60 14 -15 -14 -10;450:-4 0 76 112 -22 60 12 -18 -16 -12;560:14 0 92 0 -50 30 34 -30 -28 -4 0 0 14;700:14 0 92 0 -50 30 34 -30 -28 -4 0 0 14;950:6 0 40 80 -20 60 14 -15 -14 -10", "epee");
            Cles(F, "Charge", "0:" + G + ";150:30 0 -60 20 -70 20 40 -60 -30 -20;300:30 0 -60 20 -70 20 -30 -20 40 -60;450:30 0 -60 20 -70 20 40 -60 -30 -20;600:16 0 90 0 50 115 24 -22 -22 -6;780:16 0 90 0 50 115 24 -22 -22 -6;1000:" + G, null, 250);

            // --- acrobaties
            const string A = "Acrobaties";
            Cles(A, "Triple salto", "0:" + S + ";180:" + Cr + ";330:-5 -10 160 10 -170 -10 8 -5 -4 -5 55 20;520:" + Boule + " 130 280;720:" + Boule + " 155 560;920:" + Boule + " 130 840;1090:5 0 80 20 70 25 40 -40 25 -35 40 1050;1200:" + Cr + " 0 1080;1420:" + S + " 0 1080", null, 80);
            Cles(A, "Salto tendu", "0:" + S + ";150:" + Cr + ";280:-5 -10 172 5 -172 -5 5 0 -5 0 40 30;440:0 0 175 0 -175 0 3 0 -3 0 86 150;600:0 0 175 0 -175 0 3 0 -3 0 80 270;720:5 0 80 20 70 25 30 -30 20 -25 26 345;820:" + Cr + " 0 360;1020:" + S + " 0 360", null, 70);
            Cles(A, "Roue sans les mains", "0:-4 -8 60 20 -60 -20 20 0 -20 0;220:0 0 40 60 -40 -60 35 0 -35 0 30 90;440:0 0 40 60 -40 -60 35 0 -35 0 48 180;660:0 0 40 60 -40 -60 35 0 -35 0 30 270;860:-4 -8 60 20 -60 -20 20 -10 -20 -10 0 360;1050:" + S + " 0 360", null, 130);
            Cles(A, "Rondade flip", "0:-4 -8 170 0 190 0 20 0 -20 0;220:0 0 170 0 190 0 35 0 -35 0 0 90;440:0 0 170 0 190 0 35 0 -35 0 0 180;660:0 0 170 0 190 0 20 -20 -20 -20 0 290;800:" + Cr + " 0 360;950:-15 -20 170 0 -175 0 8 -5 -4 -5 30 335;1100:" + Boule + " 74 210;1250:" + Boule + " 66 80;1380:5 0 80 20 70 25 40 -40 25 -35 20 10;1480:" + Cr + ";1680:" + S, null, 70);
            Cles(A, "Équilibre sur la tête", "0:" + S + ";300:80 10 150 0 145 0 30 -40 -30 0 0 40;650:0 0 150 100 155 95 4 0 -4 0 0 180;1300:0 0 150 100 155 95 50 0 -50 0 0 180;1950:0 0 150 100 155 95 4 0 -4 0 0 180;2300:80 10 150 0 145 0 30 -40 -30 0 0 40;2600:" + S);
            Cles(A, "Grand jeté", "0:" + S + ";160:10 0 -40 20 60 20 40 -60 -30 -20;320:0 -10 150 0 -150 0 85 0 -85 0 44;500:0 -10 150 0 -150 0 85 0 -85 0 56;660:8 0 60 30 50 30 40 -50 -20 -40 18;780:" + Cr + ";960:" + S, null, 190);
            Ajouter("Danses", "Vague du corps", 1.1, 5, t =>
            {
                double f = 2 * Math.PI * t;
                double[] p = L(S);
                p[I.Torse] = 14 * Math.Sin(f); p[I.Tete] = 10 * Math.Sin(f - 1); p[I.Ha1] = 6 + 6 * Math.Sin(f + 1); p[I.Ha2] = -6 + 6 * Math.Sin(f + 1);
                p[I.Ge1] = p[I.Ge2] = -14 * (1 + Math.Sin(f + 2)); p[I.Ep1] = 20 + 20 * Math.Sin(f - 2); p[I.Ep2] = -20 + 20 * Math.Sin(f - 2);
                return p;
            }).Objet = "note";

            // --- sport
            const string P = "Sport";
            Osc(P, "Tractions", "0 -10 178 0 180 0 2 -10 -2 -30 30", "0 -10 70 110 72 108 10 -30 6 -50 56", 1.6, 5, "barre");
            Ajouter(P, "Mountain climbers", 0.5, 8, t =>
            {
                double s = Math.Sin(2 * Math.PI * t);
                double[] p = L(Planche);
                p[I.Ha1] = 70 * Math.Max(0, s); p[I.Ge1] = -90 * Math.Max(0, s); p[I.Ha2] = 2 + 70 * Math.Max(0, -s); p[I.Ge2] = -90 * Math.Max(0, -s);
                return p;
            });
            Cles(P, "Haltérophilie", "0:" + S + ";300:40 -10 20 0 22 0 70 -115 60 -108;700:10 -5 60 130 62 130 30 -50 20 -45;1000:0 -8 178 0 180 0 8 0 -8 0;1800:0 -8 178 0 180 0 10 -4 -10 -4;2100:30 0 20 0 22 0 50 -80 40 -75;2400:" + S, "haltere");
            Osc(P, "Fentes sautées", "5 0 -35 105 -45 112 70 -85 -40 -75", "5 0 -35 105 -45 112 -40 -75 70 -85 14", 0.8, 6);
            Cles(P, "Kata de karaté", "0:" + G + ";350:16 0 90 0 50 115 24 -22 -22 -6 0 0 8;700:6 0 110 80 100 85 14 -15 -14 -10;1050:20 10 50 60 40 70 25 -40 -10 -35;1400:-16 0 55 100 30 110 92 -4 -8 -10;1750:" + G + ";2100:45 15 60 80 55 85 6 0 -6 0;2600:" + S);
            Cles(P, "Tai-chi", "0:" + S + ";900:4 0 70 20 30 60 20 -30 -14 -24;1800:8 0 100 10 -40 40 34 -40 -20 -16 0 0 6;2700:0 0 40 70 80 20 14 -30 -20 -30 0 0 -4;3600:-4 0 -30 40 110 10 -10 -20 26 -36 0 0 -8;4500:" + S);
            Ajouter(P, "Rameur", 1.4, 5, t =>
            {
                double c = Math.Cos(2 * Math.PI * t), d = (1 + c) / 2;
                double[] p = L("0 0 0 0 0 0 0 0 0 0");
                p[I.Ha1] = 95 + 20 * d; p[I.Ge1] = -10 - 70 * d; p[I.Ha2] = 92 + 20 * d; p[I.Ge2] = -8 - 70 * d;
                p[I.Torse] = -22 + 34 * d; p[I.Ep1] = 78 - 30 * (1 - d); p[I.Co1] = 4 + 80 * (1 - d); p[I.Ep2] = 74 - 30 * (1 - d); p[I.Co2] = 6 + 80 * (1 - d);
                return p;
            });
            Cles(P, "Smash de volley", "0:" + S + ";200:" + Cr + ";400:-12 -15 -60 60 150 10 10 -30 -10 -50 46;520:14 0 110 0 60 40 20 -20 -10 -40 52;700:20 5 40 10 30 30 30 -50 20 -45 20;820:" + Cr + ";1000:" + S, "ballontir");
            Osc(P, "Jongle avec un ballon", "-4 8 20 30 -30 30 30 -20 -6 -4", "-4 8 20 30 -30 30 72 -62 -6 -4", 0.5, 10, "ballonjongle");
            Ajouter(P, "Hula hoop", 0.6, 8, t =>
            {
                double f = 2 * Math.PI * t;
                double[] p = L("0 0 120 60 -120 -60 8 -8 -8 -8");
                p[I.X] = 6 * Math.Sin(f); p[I.Torse] = -4 * Math.Sin(f);
                return p;
            }).Objet = "cerceau";
            Osc(P, "Ski (schuss)", "40 -25 -40 100 -45 100 70 -110 64 -104", "44 -22 -36 104 -41 104 76 -118 70 -112", 0.9, 4, "planche");

            // --- vie quotidienne
            const string Q = "Quotidien";
            Osc(Q, "Fait ses lacets", "55 25 30 20 35 15 80 -120 -10 -100", "57 27 34 16 31 19 80 -120 -10 -100", 0.4, 8);
            Osc(Q, "Balaie", "20 10 40 10 30 110 12 -10 -10 -6", "22 12 15 15 26 112 14 -12 -8 -8", 0.8, 6, "balai");
            Osc(Q, "Parapluie", "0 0 60 110 -8 12 6 0 -6 0", "0 2 61 110 -8 12 6 0 -6 0", 1.6, 3, "parapluie");
            Osc(Q, "Joue de la guitare", "-6 0 -10 80 -50 -70 10 -8 -8 -4", "-6 4 -10 118 -50 -66 10 -10 -8 -6", 0.3, 12, "guitare").Son = "note";
            Osc(Q, "Chante au micro", "-8 -15 70 130 -40 20 8 0 -8 0", "-14 -24 72 132 -60 10 10 -6 -8 -4", 0.8, 5, "micro");
            Osc(Q, "DJ", "10 10 100 130 60 30 10 -8 -8 -8", "12 14 100 132 72 10 12 -12 -8 -10", 0.4, 10, "note");
            Osc(Q, "Joue à la console", "5 20 55 60 60 55 90 0 84 0", "7 22 58 57 57 58 90 0 84 0", 0.25, 14, "telephone");
            Osc(Q, "Lit le journal assis", "-8 20 50 80 55 75 90 0 84 0", "-8 23 51 80 56 75 90 0 84 0", 1.8, 3, "livre");
            Osc(Q, "A le hoquet", S, "-4 -10 12 14 -12 14 6 0 -6 0 7", 0.9, 5).Son = "pop";
            Autre(Osc("Gestes", "Mange", "4 8 40 60 -8 12", "6 14 75 135 -8 12", 0.6, 5));
            Autre(Osc("Gestes", "Tousse", "20 20 80 130 -8 12", "10 10 80 125 -8 12", 0.3, 5));
            Osc("Gestes", "Se frotte les yeux", "8 20 85 140 90 135", "8 22 88 138 87 137", 0.3, 6);
            Autre(Osc("Gestes", "Fait du stop", "0 0 70 30 -8 12", "0 0 76 18 -8 12", 0.8, 3));
            Osc("Gestes", "Prend une photo", "6 10 80 110 75 115", "6 12 81 110 76 115", 1.2, 2, "telephone").Son = "pop";
            Autre(Osc("Gestes", "Ouf !", "0 -5 120 120 -8 12", "0 -5 126 100 -8 12", 0.6, 2));
            Autre(Osc("Gestes", "Se gratte la nuque", "6 10 165 110 -8 12", "6 10 165 126 -8 12", 0.3, 5));
            Osc("Gestes", "Câlin", "-4 -5 80 -10 -80 10", "6 5 60 80 55 85", 1.0, 2, "coeur");
            Osc("Gestes", "Se ronge les ongles", "10 15 85 135 20 95", "10 17 87 133 20 95", 0.15, 12);
            Anim peche = Osc("Fenêtres", "Pêche depuis le bord", "-8 5 60 30 50 40 22 -4 4 -32 -44", "-10 2 64 34 54 44 20 -6 6 -30 -44", 2.0, 5, "canne");
            peche.SurFenetre = true;

            // --- émotions
            const string E = "Émotions";
            Osc(E, "Fou rire", "0 0 60 60 70 50 60 -80 20 -20 0 -75", "5 5 70 50 60 60 20 -20 60 -80 0 -68", 0.3, 10);
            Osc(E, "Caprice", "0 -10 150 60 170 10 -15 -60 -12 0 0 75", "0 -8 170 10 150 60 -15 0 -12 -60 0 75", 0.25, 10, "exclam");
            Osc(E, "Choqué", "-10 -15 150 100 -150 -100 8 0 -8 0", "-12 -18 152 104 -152 -104 10 -4 -10 -4", 0.5, 4, "exclam");
            Osc(E, "Supplie", "20 -20 75 60 80 55 5 -95 -5 -90", "24 -24 78 58 83 53 5 -95 -5 -90", 0.5, 5);
            Osc(E, "Triomphe à genoux", "-20 -25 165 -5 -165 5 10 -100 0 -95", "-23 -28 168 -2 -168 2 10 -100 0 -95", 0.7, 4);
            Cles(E, "Dépité", "0:" + S + ";300:0 -6 35 80 25 85 6 0 -6 0;700:0 4 15 30 5 35 6 0 -6 0;1100:18 30 5 5 -5 5 6 -5 -6 -5;2000:20 34 5 5 -5 5 6 -6 -6 -6;2400:" + S);
            Osc(E, "Shoote dans un caillou", "8 20 -30 -40 -35 -45 6 0 -6 0", "8 22 -30 -40 -35 -45 40 -10 -6 0", 0.9, 3);
            Osc(E, "Surexcité", "0 -10 150 40 -150 -40 10 -20 -10 -20 0 0 -8", "0 -12 160 20 -160 -20 20 -40 -20 -40 30 0 8", 0.35, 8, "note");
        }

        // ------------------------------------------------------------ deuxième fournée : fenêtres, pouvoirs, numéros

        static void Nouveautes()
        {
            // sur une fenêtre : les jambes pendent dans le vide (hauteur négative = sous la ligne du sol)
            Anim bord = Osc("Fenêtres", "Assis au bord, balance les jambes", "-8 0 -25 -10 -35 -10 22 -4 4 -32 -44", "-8 3 -25 -10 -35 -10 4 -32 22 -4 -44", 1.0, 8);
            bord.SurFenetre = true;
            Anim reve = Osc("Fenêtres", "Assis au bord, rêvasse", "-14 -18 -30 -12 -40 -12 14 -8 8 -14 -44", "-16 -22 -30 -12 -40 -12 12 -10 10 -12 -44", 2.4, 4, "note");
            reve.SurFenetre = true;
            Anim guette = Osc("Fenêtres", "Regarde en bas", "55 25 30 5 20 10 75 -120 60 -110", "62 30 34 5 24 10 78 -124 62 -112", 1.6, 3);
            guette.SurFenetre = true;
            Anim allonge = Osc("Fenêtres", "Allongé au bord, un bras dans le vide", "0 5 -72 0 28 -5 15 0 12 0 -36 -75", "0 8 -58 0 28 -5 15 0 12 0 -34 -75", 2.2, 4);
            allonge.SurFenetre = true;
            Anim funambule = Pas("Funambule", 18, 22, 0, 0, 0, 0, 30, 1.3, 6);
            funambule.Famille = "Fenêtres";
            if (!Familles.Contains("Fenêtres")) Familles.Add("Fenêtres");

            // d'autres façons d'avancer
            Anim rampe = Ajouter("Déplacements", "Rampe", 1.1, 1, t =>
            {
                double s = Math.Sin(2 * Math.PI * t);
                double[] p = L("0 -25 0 0 0 0 0 0 0 0 0 78");
                p[I.Ep1] = 150 + 30 * s; p[I.Co1] = 40 - 30 * s; p[I.Ep2] = 150 - 30 * s; p[I.Co2] = 40 + 30 * s;
                p[I.Ha1] = -10 + 30 * Math.Max(0, s); p[I.Ge1] = -70 * Math.Max(0, s); p[I.Ha2] = -10 + 30 * Math.Max(0, -s); p[I.Ge2] = -70 * Math.Max(0, -s);
                return p;
            });
            rampe.Deplace = true; rampe.Vitesse = 28;
            Anim roule = Ajouter("Déplacements", "Roule en boule", 0.55, 1, t => { double[] p = L(Boule); p[I.Rot] = 360 * t; return p; });
            roule.Deplace = true; roule.Vitesse = 170; roule.Son = "swish";
            Anim grenouille = Ajouter("Déplacements", "Sauts de grenouille", 0.8, 1, t =>
            {
                double u = Math.Max(0, Math.Sin(2 * Math.PI * t));
                double[] p = L(Cr);
                p[I.Air] = 34 * u; p[I.Ha1] = 62 - 40 * u; p[I.Ge1] = -112 + 90 * u; p[I.Ha2] = 52 - 60 * u; p[I.Ge2] = -104 + 80 * u; p[I.Ep1] = -30 + 150 * u; p[I.Ep2] = -40 + 150 * u;
                return p;
            });
            grenouille.Deplace = true; grenouille.Vitesse = 95; grenouille.Son = "saut";
            Anim patine = Pas("Patinage", 34, 12, 40, 10, 14, 0, 150, 1.5);
            patine.Objet = "planche";

            // pouvoirs et armes
            const string F = "Combat";
            Cles(F, "Rayon d'énergie", "0:" + G + ";400:-12 0 -35 70 -25 80 20 -30 -18 -25;800:-14 0 -38 72 -28 82 22 -34 -18 -28;900:18 0 92 2 86 6 28 -26 -26 -6 0 0 10;1700:20 0 93 2 87 6 30 -28 -26 -6 0 0 8;2000:" + G, "rayon");
            Cles(F, "Tir à l'arc", "0:" + S + ";300:-4 0 20 120 92 2 14 -6 -16 -4;700:-8 0 -30 150 92 2 16 -8 -16 -4;800:-6 0 10 100 92 2 16 -8 -16 -4;1200:-6 0 10 100 92 2 16 -8 -16 -4;1500:" + S, "arc");
            Cles(F, "Lance un shuriken", "0:" + G + ";200:-10 -5 -100 70 60 60 -10 -12 16 -10;320:16 0 95 5 -40 40 28 -24 -22 -6 0 0 8;700:16 0 60 10 -40 40 28 -24 -22 -6 0 0 8;950:" + G, "shuriken");
            Osc(F, "Bouclier", "8 0 75 95 -20 60 20 -22 -16 -14", "10 2 78 92 -22 62 22 -26 -16 -16", 0.7, 4, "bouclier");
            Ajouter(F, "Toupie de combat", 0.45, 5, t => { double[] p = L("0 0 90 0 -90 0 60 -30 -20 -10"); p[I.Ep1] = 90 + 360 * t; p[I.Ep2] = -90 + 360 * t; p[I.Air] = 6; return p; }).Objet = "baton";

            // numéros
            const string A = "Acrobaties";
            Cles(A, "Double saut", "0:" + S + ";150:" + Cr + ";300:-4 -10 172 5 -172 -5 5 0 -5 0 34;440:" + Boule + " 42;560:-4 -10 150 20 -150 -20 20 -40 -10 -30 60;700:-4 -10 172 5 -172 -5 5 0 -5 0 92;860:8 0 60 30 50 30 30 -50 20 -45 50;1000:" + Cr + ";1200:" + S);
            Ajouter(A, "Moulin à vent", 0.7, 5, t => { double[] p = L("0 0 140 20 -140 -20 50 -10 -50 -10"); p[I.Rot] = 360 * t; return p; }).Son = "swish";
            Ajouter(A, "Le ver", 1.0, 4, t =>
            {
                double f = 2 * Math.PI * t;
                double[] p = L("0 0 150 60 152 58 -15 0 -12 0 0 78");
                p[I.Torse] = -18 * Math.Sin(f); p[I.Ha1] = -15 + 22 * Math.Sin(f + 1.5); p[I.Ha2] = -12 + 22 * Math.Sin(f + 1.5); p[I.Air] = 5 * Math.Max(0, Math.Sin(f + 0.8));
                return p;
            }).Vitesse = 30;
            Cles(A, "Glissade sur les genoux", "0:" + S + ";200:10 0 60 30 50 30 40 -60 20 -50;420:-25 -20 160 -10 -160 10 10 -100 0 -95;1100:-28 -22 165 -6 -165 6 10 -100 0 -95;1500:" + S, "note", 190);
            Ajouter("Quotidien", "Jongle", 0.6, 8, t =>
            {
                double s = Math.Sin(2 * Math.PI * t);
                double[] p = L("0 -12 0 0 0 0 8 0 -8 0");
                p[I.Ep1] = 40 + 12 * s; p[I.Co1] = 80 - 14 * s; p[I.Ep2] = 30 - 12 * s; p[I.Co2] = 85 + 14 * s;
                return p;
            }).Objet = "jongle";
            Osc("Quotidien", "Joue au yo-yo", "4 12 70 20 -10 12 8 0 -8 0", "4 14 60 40 -10 12 8 0 -8 0", 0.7, 8, "yoyo");
            Osc("Quotidien", "Prend un selfie", "-4 -8 115 15 -35 105 10 0 -8 0", "-6 -10 118 12 -35 105 14 -6 -8 0", 1.0, 3, "telephone");
            Osc("Émotions", "Cœur avec les bras", "0 -8 160 70 -160 -70 8 0 -8 0", "0 -10 163 74 -163 -74 8 0 -8 0 3", 0.8, 4, "coeur");
            Cles("Émotions", "Saute de joie en tournant", "0:" + S + ";150:" + Cr + ";300:-4 -10 172 5 -172 -5 5 0 -5 0 30;430:" + Boule + " 52 180;560:-4 -10 172 5 -172 -5 5 0 -5 0 30 345;680:" + Cr + " 0 360;850:-6 -15 160 -10 -160 10 8 0 -8 0 0 360;1300:-8 -18 165 -5 -165 5 8 0 -8 0 0 360;1500:" + S + " 0 360", "note");
        }

        // Les bruitages : attribués d'après la famille et le nom, pour ne pas les répéter sur 300 lignes.
        static void Sonoriser()
        {
            foreach (Anim a in Toutes)
            {
                if (a.Son != null) continue;
                string n = a.Nom;
                switch (a.Famille)
                {
                    case "Combat":
                        if (n.StartsWith("Épée") || n.StartsWith("Bâton") || n.StartsWith("Lance") || n.StartsWith("Toupie")) Bruit(a, "swish", 0.3);
                        else if (n.Contains("énergie") || n.StartsWith("Onde") || n.StartsWith("Rayon")) Bruit(a, "energie", 0.05);
                        else if (n.StartsWith("Tir")) Bruit(a, "swish", 0.5);
                        else if (!n.StartsWith("Garde") && !n.StartsWith("Sautille") && !n.StartsWith("Provocation") && !n.StartsWith("Salut") && !n.StartsWith("Esquive") && !n.StartsWith("Bouclier")) Bruit(a, "coup", 0.4);
                        break;
                    case "Acrobaties":
                        if (n.StartsWith("Roulade") || n.StartsWith("Roue") || n.StartsWith("Toupie")) Bruit(a, "swish", 0.2);
                        else if (n.StartsWith("Sa") || n.StartsWith("Double") || n.StartsWith("Flip") || n.StartsWith("Kip") || n.StartsWith("Plongeon")) Bruit(a, "saut", 0.15);
                        break;
                    case "Émotions":
                        if (n.StartsWith("Victoire") || n.StartsWith("Joie") || n.StartsWith("Danse") || n.StartsWith("Saute")) Bruit(a, "tada", 0.1);
                        else if (n.StartsWith("Surprise")) Bruit(a, "pop", 0.05);
                        break;
                    case "Quotidien":
                        if (n.StartsWith("Glisse") || n.StartsWith("Trébuche")) Bruit(a, "glisse", 0.05);
                        else if (n.StartsWith("Éternue")) Bruit(a, "coup", 0.45);
                        else if (n.StartsWith("Siffle")) Bruit(a, "note", 0.1);
                        break;
                    case "Sport":
                        if (n.StartsWith("Tir") || n.StartsWith("Lancer") || n.StartsWith("Bowling") || n.StartsWith("Swing") || n.StartsWith("Service")) Bruit(a, "swish", 0.35);
                        else if (n.StartsWith("Burpee")) Bruit(a, "saut", 0.7);
                        break;
                    case "Gestes":
                        if (n.StartsWith("Yes") || n.StartsWith("Idée")) Bruit(a, "note", 0.1);
                        break;
                }
            }
        }

        static void Bruit(Anim a, string son, double quand)
        {
            a.Son = son;
            a.SonA = quand;
        }

        // ------------------------------------------------------------ attentes (entre deux actions)

        static void Attentes()
        {
            Repos.Add(Ajouter(null, "Debout", 3.2, 1, t =>
            {
                double[] p = L(S);
                double r = Math.Sin(2 * Math.PI * t) * R.D("respiration") / 100;
                p[I.Torse] += 1.5 * r; p[I.Ep1] += 2 * r; p[I.Ep2] -= 2 * r; p[I.Co1] += 2 * r; p[I.Tete] -= r;
                return p;
            }));
            Repos.Add(Ajouter(null, "Décontracté", 4.0, 1, t =>
            {
                double[] p = L("-2 0 4 8 -14 16 10 -6 -8 0");
                double r = Math.Sin(2 * Math.PI * t) * R.D("respiration") / 100;
                p[I.Torse] += 2 * r; p[I.Ha1] += 2 * r; p[I.Ge1] -= 3 * Math.Abs(r);
                return p;
            }));
            Repos.Add(Ajouter(null, "Poids sur une jambe", 4.4, 1, t =>
            {
                double[] p = L("3 2 -30 100 -12 14 2 0 -14 -12");
                p[I.Torse] += 1.5 * Math.Sin(2 * Math.PI * t) * R.D("respiration") / 100;
                return p;
            }));
        }

        // ------------------------------------------------------------ déplacements

        // A = écart des jambes, K = levée du genou, B = balancier des bras, C = pli du coude.
        static Anim Pas(string nom, double A, double K, double B, double C, double penche, double rebond, double v, double T,
            int bras = 0, double flex = 0, double assise = 0, bool robot = false)
        {
            Anim a = Ajouter("Déplacements", nom, T, 1, t =>
            {
                if (robot) t = Math.Floor(t * 8) / 8;
                double f = 2 * Math.PI * t, s = Math.Sin(f), c = Math.Cos(f);
                var p = new double[I.N];
                p[I.Ha1] = A * s + assise; p[I.Ha2] = -A * s + assise;
                p[I.Ge1] = -(flex + K * Math.Max(0, c)); p[I.Ge2] = -(flex + K * Math.Max(0, -c));
                p[I.Torse] = penche + 2 * Math.Sin(2 * f); p[I.Tete] = -penche * 0.4;
                p[I.Air] = rebond * Math.Abs(s);
                switch (bras)
                {
                    case 0: p[I.Ep1] = -B * s; p[I.Ep2] = B * s; p[I.Co1] = C + 8 * Math.Max(0, -s); p[I.Co2] = C + 8 * Math.Max(0, s); break;
                    case 1: p[I.Ep1] = 88 + 4 * s; p[I.Ep2] = 82 - 4 * s; p[I.Co1] = 5; p[I.Co2] = 8; break;                 // bras tendus devant
                    case 2: p[I.Ep1] = -75; p[I.Ep2] = -68; p[I.Co1] = -8; p[I.Co2] = -8; break;                             // bras en arrière
                    case 3: p[I.Ep1] = 165 + 15 * Math.Sin(3 * f); p[I.Ep2] = -165 + 15 * Math.Sin(3 * f + 2); p[I.Co1] = 15; p[I.Co2] = -15; break;
                    case 4: p[I.Ep1] = -35; p[I.Co1] = 105; p[I.Ep2] = -45; p[I.Co2] = 112; break;                           // mains sur les hanches
                    case 5: p[I.Ep1] = 50; p[I.Co1] = 110; p[I.Ep2] = 40; p[I.Co2] = 118; break;                             // mains repliées
                    case 6: p[I.Ep1] = 90 + 14 * Math.Sin(2 * f); p[I.Ep2] = -90 + 14 * Math.Sin(2 * f); p[I.Torse] += 4 * Math.Sin(2 * f); break;   // bras en balancier
                    case 7: p[I.Ep1] = 30 + 40 * Math.Sin(f + 1); p[I.Ep2] = -30 + 40 * Math.Sin(f + 2.5); p[I.Co1] = 20; p[I.Co2] = 20; p[I.Torse] += 14 * s; p[I.Tete] = 10 * Math.Sin(f + 0.8); break;   // titube
                }
                return p;
            });
            a.Deplace = true;
            a.Vitesse = v;
            return a;
        }

        static void Deplacements()
        {
            Marche = Pas("Marche", 26, 40, 24, 15, 4, 0, 70, 0.9);
            Pas("Marche lente", 16, 26, 12, 10, 2, 0, 35, 1.4);
            Pas("Marche rapide", 32, 50, 40, 60, 8, 0, 120, 0.6);
            Pas("Petit trot", 32, 70, 30, 85, 10, 4, 150, 0.55);
            Course = Pas("Course", 45, 95, 50, 90, 16, 7, 240, 0.45);
            Pas("Sprint", 55, 110, 65, 95, 24, 10, 360, 0.36);
            Pas("Pas de loup", 30, 60, 10, 70, 22, 0, 40, 1.3, 0, 25, 20);
            Pas("Marche militaire", 40, 12, 55, 5, 0, 0, 80, 0.8);
            Pas("Genoux hauts", 42, 110, 40, 90, -4, 3, 60, 0.6);
            Pas("Zombie", 14, 15, 0, 0, 12, 0, 25, 1.6, 1);
            Pas("Course ninja", 50, 100, 0, 0, 40, 5, 320, 0.4, 2);
            Pas("Marche accroupie", 22, 30, 10, 60, 30, 0, 45, 0.9, 0, 70, 45);
            Pas("Moonwalk", 22, 8, 10, 10, -4, 0, -60, 1.0);
            Pas("Marche arrière", 20, 30, 15, 15, -3, 0, -45, 1.0);
            Pas("Sautille", 30, 80, 35, 60, 4, 14, 110, 0.6);
            Pas("Démarche cool", 22, 30, 32, 20, -6, 2, 55, 1.1);
            Pas("Sur la pointe des pieds", 14, 30, 0, 0, 0, 3, 45, 0.5, 5);
            Pas("Fuite paniquée", 45, 90, 0, 0, 12, 6, 260, 0.4, 3);
            Pas("Mains sur les hanches (marche)", 24, 35, 0, 0, -3, 0, 60, 1.0, 4);
            Pas("Robot", 24, 40, 30, 80, 0, 0, 50, 1.0, 0, 0, 0, true);
            Pas("Pas de géant", 50, 30, 45, 10, 6, 2, 110, 1.2);
            Pas("Petits pas pressés", 12, 25, 20, 80, 6, 1, 90, 0.3);
        }

        // ------------------------------------------------------------ danses : un mouvement de bras x un mouvement de jambes

        static void Danses()
        {
            var nomsBras = new[] { "Poings en l'air", "Disco", "Vague", "Mains en l'air", "Moulinets", "Déhanché", "Tape des mains", "Boxe",
                "Twist des bras", "Robot", "Guitare", "Lasso", "Égyptien", "Poulet", "Dab", "Rouleau",
                "Floss", "YMCA", "Essuie-glaces", "Pom-pom",
                "Macarena", "Hélicoptère", "Cadres", "Brasse", "Fièvre du samedi soir", "Tape des cuisses", "Maracas", "Bras qui se balancent",
                "Manivelle", "Papillon", "Clap haut-bas", "Roulé d'épaules", "Pointe gauche-droite", "Vague à deux bras",
                "Pluie", "Pêche à la ligne", "Caddie", "Pousse le plafond", "Poing qui pompe", "Bisous", "Batterie", "Mains sur la tête",
                "Ciseaux", "Balancier", "Chef d'orchestre", "Thriller", "Tire la corde", "Soleil", "Pagaie", "Confettis", "Vitres", "Cloche" };
            // les quatre temps de la macarena : bras tendus, mains aux épaules, mains sur la tête, mains aux hanches
            var macarena = new[] { new double[] { 90, 0, 86, 0 }, new double[] { 70, 150, 66, 150 }, new double[] { 150, 110, 146, 110 }, new double[] { 30, 100, -30, -100 } };
            // les quatre lettres de YMCA : (épaule1, coude1, épaule2, coude2)
            var lettres = new[] { new double[] { 150, 0, -150, 0 }, new double[] { 120, 120, -120, -120 }, new double[] { 150, 50, 40, -40 }, new double[] { 172, 25, -172, -25 } };
            var bras = new Action<double, double, double, double[]>[]
            {
                (f, s, c, p) => { p[I.Ep1] = 125 + 45 * s; p[I.Co1] = 35; p[I.Ep2] = 125 - 45 * s; p[I.Co2] = 35; },
                (f, s, c, p) => { p[I.Ep1] = 85 + 70 * s; p[I.Co1] = 8; p[I.Ep2] = -25 - 10 * s; p[I.Co2] = 100; p[I.Tete] -= 8 * s; },
                (f, s, c, p) => { p[I.Ep1] = 90 + 35 * s; p[I.Co1] = 30 * Math.Sin(f - 1); p[I.Ep2] = -90 - 35 * s; p[I.Co2] = -30 * Math.Sin(f + 2.1); },
                (f, s, c, p) => { p[I.Ep1] = 165 + 14 * s; p[I.Co1] = 12; p[I.Ep2] = -165 + 14 * s; p[I.Co2] = -12; },
                (f, s, c, p) => { p[I.Ep1] = f * 57.2958; p[I.Co1] = 15; p[I.Ep2] = f * 57.2958 + 180; p[I.Co2] = 15; },
                (f, s, c, p) => { p[I.Ep1] = -35; p[I.Co1] = 105; p[I.Ep2] = -45; p[I.Co2] = 112; p[I.Torse] += 6 * s; },
                (f, s, c, p) => { p[I.Ep1] = 155 + 18 * c; p[I.Co1] = 18; p[I.Ep2] = 205 - 18 * c; p[I.Co2] = -18; },
                (f, s, c, p) => { p[I.Ep1] = 75; p[I.Co1] = 95 - 90 * Math.Max(0, s); p[I.Ep2] = 65; p[I.Co2] = 100 - 95 * Math.Max(0, -s); },
                (f, s, c, p) => { p[I.Ep1] = 45 + 35 * s; p[I.Co1] = 85; p[I.Ep2] = 45 - 35 * s; p[I.Co2] = 85; },
                (f, s, c, p) => { double q = Math.Round(s); p[I.Ep1] = 90 * Math.Max(0, q); p[I.Co1] = 90; p[I.Ep2] = 90 * Math.Max(0, -q); p[I.Co2] = 90; },
                (f, s, c, p) => { p[I.Ep1] = 35; p[I.Co1] = 75 + 25 * Math.Sin(4 * f); p[I.Ep2] = 105; p[I.Co2] = 75; p[I.Torse] -= 8; p[I.Tete] += 10 * Math.Sin(2 * f); },
                (f, s, c, p) => { p[I.Ep1] = 165 + 18 * s; p[I.Co1] = 45 + 40 * c; p[I.Ep2] = -30; p[I.Co2] = 100; },
                (f, s, c, p) => { p[I.Ep1] = 92 + 6 * s; p[I.Co1] = 88; p[I.Ep2] = -92 + 6 * s; p[I.Co2] = 88; p[I.Tete] += 6 * s; },
                (f, s, c, p) => { p[I.Ep1] = 35 + 40 * Math.Abs(s); p[I.Co1] = 140; p[I.Ep2] = -35 - 40 * Math.Abs(s); p[I.Co2] = -140; },
                (f, s, c, p) => { double w = (1 + Math.Tanh(4 * s)) / 2; p[I.Ep1] = 118 + 14 * w; p[I.Co1] = 150 - 150 * w; p[I.Ep2] = 132 - 14 * w; p[I.Co2] = 150 * w; p[I.Tete] += 26; p[I.Torse] += 16; },
                (f, s, c, p) => { p[I.Ep1] = 70 + 15 * Math.Sin(2 * f); p[I.Co1] = 95 + 15 * Math.Cos(2 * f); p[I.Ep2] = 70 - 15 * Math.Sin(2 * f); p[I.Co2] = 95 - 15 * Math.Cos(2 * f); },
                (f, s, c, p) => { double w = Math.Sin(2 * f); p[I.Ep1] = 40 * w; p[I.Co1] = 8; p[I.Ep2] = 40 * w - 12; p[I.Co2] = 8; p[I.X] -= 5 * w; },
                (f, s, c, p) =>
                {
                    double t = f / (2 * Math.PI) * 4, k = Math.Max(0, (t - Math.Floor(t) - 0.7) / 0.3);
                    double[] a = lettres[(int)Math.Floor(t) % 4], b = lettres[((int)Math.Floor(t) + 1) % 4];
                    k = k * k * (3 - 2 * k);
                    p[I.Ep1] = a[0] + (b[0] - a[0]) * k; p[I.Co1] = a[1] + (b[1] - a[1]) * k; p[I.Ep2] = a[2] + (b[2] - a[2]) * k; p[I.Co2] = a[3] + (b[3] - a[3]) * k;
                },
                (f, s, c, p) => { p[I.Ep1] = 90; p[I.Co1] = 90 + 35 * s; p[I.Ep2] = 80; p[I.Co2] = 90 + 35 * s; },
                (f, s, c, p) => { double w = (1 + Math.Tanh(4 * s)) / 2; p[I.Ep1] = -35 + 205 * w; p[I.Co1] = 105 - 105 * w; p[I.Ep2] = 170 - 215 * w; p[I.Co2] = 105 * w; },
                (f, s, c, p) =>
                {
                    double t = f / (2 * Math.PI) * 4, k = Math.Max(0, (t - Math.Floor(t) - 0.6) / 0.4);
                    double[] a = macarena[(int)Math.Floor(t) % 4], b = macarena[((int)Math.Floor(t) + 1) % 4];
                    k = k * k * (3 - 2 * k);
                    p[I.Ep1] = a[0] + (b[0] - a[0]) * k; p[I.Co1] = a[1] + (b[1] - a[1]) * k; p[I.Ep2] = a[2] + (b[2] - a[2]) * k; p[I.Co2] = a[3] + (b[3] - a[3]) * k;
                },
                (f, s, c, p) => { p[I.Ep1] = 165; p[I.Co1] = 70 * s; p[I.Ep2] = -20; p[I.Co2] = 20; p[I.Tete] -= 10; },
                (f, s, c, p) => { p[I.Ep1] = 120 + 30 * s; p[I.Co1] = 100; p[I.Ep2] = 120 - 30 * s; p[I.Co2] = 100; },
                (f, s, c, p) => { p[I.Ep1] = 90 + 60 * c; p[I.Co1] = 60 - 60 * c; p[I.Ep2] = 86 + 60 * c; p[I.Co2] = 64 - 60 * c; p[I.Torse] += 6 * c; },
                (f, s, c, p) => { double w = (1 + s) / 2; p[I.Ep1] = 30 + 125 * w; p[I.Co1] = 10; p[I.Ep2] = -30 - 20 * s; p[I.Co2] = 100; p[I.Torse] -= 6 * s; },
                (f, s, c, p) => { p[I.Ep1] = 40 + 60 * Math.Max(0, s); p[I.Co1] = 70 - 40 * Math.Max(0, s); p[I.Ep2] = 40 + 60 * Math.Max(0, -s); p[I.Co2] = 70 - 40 * Math.Max(0, -s); },
                (f, s, c, p) => { p[I.Ep1] = 80; p[I.Co1] = 100 + 30 * Math.Sin(4 * f); p[I.Ep2] = 70; p[I.Co2] = 100 - 30 * Math.Sin(4 * f); },
                (f, s, c, p) => { p[I.Ep1] = 150 + 25 * s; p[I.Co1] = -20 * s; p[I.Ep2] = 150 - 25 * s; p[I.Co2] = 20 * s; p[I.Tete] += 8 * s; },
                (f, s, c, p) => { p[I.Ep1] = 80 + 25 * c; p[I.Co1] = 60 + 30 * s; p[I.Ep2] = 80 - 25 * c; p[I.Co2] = 60 - 30 * s; },
                (f, s, c, p) => { p[I.Ep1] = f * 57.2958; p[I.Co1] = 20; p[I.Ep2] = f * 57.2958 + 8; p[I.Co2] = 20; p[I.Torse] += 6 * s; },
                (f, s, c, p) => { p[I.Ep1] = 95 + 75 * s; p[I.Co1] = 15; p[I.Ep2] = 99 + 75 * s; p[I.Co2] = 15; },
                (f, s, c, p) => { p[I.Ep1] = 10 + 22 * s; p[I.Co1] = 30 + 22 * c; p[I.Ep2] = -10 + 22 * c; p[I.Co2] = 30 + 22 * s; p[I.Torse] += 8 * s; p[I.Tete] -= 8 * s; },
                (f, s, c, p) => { p[I.Ep1] = 90 + 62 * Math.Max(0, s); p[I.Co1] = 0; p[I.Ep2] = -90 - 62 * Math.Max(0, -s); p[I.Co2] = 0; p[I.Tete] += 10 * s; },
                (f, s, c, p) => { p[I.Ep1] = 100 + 40 * s; p[I.Co1] = 30 * Math.Sin(f - 1.2); p[I.Ep2] = 100 + 40 * Math.Sin(f - 0.6); p[I.Co2] = 30 * Math.Sin(f - 1.8); },
                // pluie : les mains descendent en frétillant ; pêche : il lance la ligne et mouline ; caddie : il pousse et attrape sur l'étagère
                (f, s, c, p) => { double d = (1 - c) / 2; p[I.Ep1] = 170 - 90 * d; p[I.Co1] = 10 + 12 * Math.Sin(6 * f); p[I.Ep2] = 166 - 90 * d; p[I.Co2] = 14 - 12 * Math.Sin(6 * f); },
                (f, s, c, p) => { p[I.Ep1] = 40 + 120 * Math.Max(0, s); p[I.Co1] = 20; p[I.Ep2] = 60; p[I.Co2] = 100 + 30 * Math.Sin(4 * f); },
                (f, s, c, p) => { p[I.Ep1] = 78; p[I.Co1] = 12; p[I.Ep2] = 78 + 80 * Math.Max(0, s); p[I.Co2] = 12 + 30 * Math.Max(0, -s); },
                (f, s, c, p) => { double d = Math.Abs(s); p[I.Ep1] = 140 + 20 * d; p[I.Co1] = 70 - 60 * d; p[I.Ep2] = 136 + 20 * d; p[I.Co2] = 74 - 60 * d; },
                (f, s, c, p) => { double d = Math.Abs(s); p[I.Ep1] = 120 + 40 * d; p[I.Co1] = 90 - 80 * d; p[I.Ep2] = -20; p[I.Co2] = 30; },
                (f, s, c, p) => { p[I.Ep1] = 100; p[I.Co1] = 150 - 140 * Math.Max(0, s); p[I.Ep2] = 96; p[I.Co2] = 150 - 140 * Math.Max(0, -s); },
                (f, s, c, p) => { double w = Math.Sin(2 * f); p[I.Ep1] = 70; p[I.Co1] = 60 + 50 * Math.Max(0, w); p[I.Ep2] = 60; p[I.Co2] = 60 + 50 * Math.Max(0, -w); p[I.Tete] += 6 * w; },
                (f, s, c, p) => { p[I.Ep1] = 150 + 12 * s; p[I.Co1] = 130; p[I.Ep2] = 146 - 12 * s; p[I.Co2] = 134; p[I.Torse] += 5 * s; },
                (f, s, c, p) => { p[I.Ep1] = 90 + 34 * s; p[I.Co1] = 0; p[I.Ep2] = 90 - 34 * s; p[I.Co2] = 0; },
                (f, s, c, p) => { p[I.Ep1] = 75 * s; p[I.Co1] = 12; p[I.Ep2] = -75 * s; p[I.Co2] = 12; },
                (f, s, c, p) => { p[I.Ep1] = 110 + 30 * s; p[I.Co1] = 40 + 30 * c; p[I.Ep2] = 110 - 30 * s; p[I.Co2] = 40 - 30 * c; p[I.Tete] -= 6; },
                (f, s, c, p) => { double w = Math.Tanh(4 * s); p[I.Ep1] = 95 * w; p[I.Co1] = 60 * w; p[I.Ep2] = 80 * w; p[I.Co2] = 70 * w; p[I.Tete] += 10 * w; },
                // il tire une corde vers le bas, une main après l'autre ; soleil : les deux bras tournent en sens inverse
                (f, s, c, p) => { double u = Math.Max(0, s), d = Math.Max(0, -s); p[I.Ep1] = 150 - 80 * u; p[I.Co1] = 20 + 60 * u; p[I.Ep2] = 146 - 80 * d; p[I.Co2] = 24 + 60 * d; },
                (f, s, c, p) => { p[I.Ep1] = f * 57.2958; p[I.Co1] = 6; p[I.Ep2] = -f * 57.2958; p[I.Co2] = -6; },
                // pagaie : les deux mains plongent d'un côté puis de l'autre ; confettis : il lance tout en l'air, encore et encore
                (f, s, c, p) => { p[I.Ep1] = 60 + 50 * s; p[I.Co1] = 30 + 20 * c; p[I.Ep2] = 40 + 50 * s; p[I.Co2] = 50 + 20 * c; p[I.Torse] += 6 * s; },
                (f, s, c, p) => { double d = Math.Abs(s); p[I.Ep1] = 60 + 110 * d; p[I.Co1] = 80 - 70 * d; p[I.Ep2] = 56 + 110 * d; p[I.Co2] = 84 - 70 * d; p[I.Tete] -= 12 * d; },
                // vitres : les deux mains font des ronds devant lui ; cloche : il tire sur la corde à deux mains, de tout son corps
                (f, s, c, p) => { p[I.Ep1] = 100 + 25 * c; p[I.Co1] = 30 + 25 * s; p[I.Ep2] = 100 - 25 * c; p[I.Co2] = 30 - 25 * s; },
                (f, s, c, p) => { p[I.Ep1] = 165 + 20 * s; p[I.Co1] = 20; p[I.Ep2] = 161 + 20 * s; p[I.Co2] = 24; p[I.Torse] += 7 * s; },
            };
            var nomsJambes = new[] { "rebond", "pas chassés", "coups de pied", "twist", "course sur place", "sauts", "squats", "talons", "genoux", "charleston", "accroupi", "fentes", "pointes", "ruades" };
            var tempos = new[] { 0.7, 0.9, 0.8, 0.8, 0.6, 0.7, 1.1, 0.8, 0.7, 0.8, 0.8, 1.0, 0.7, 0.7 };
            var jambes = new Action<double, double, double, double[]>[]
            {
                (f, s, c, p) => { double d = Math.Abs(s); p[I.Ha1] = 8 + 14 * d; p[I.Ge1] = -28 * d; p[I.Ha2] = -8 + 14 * d; p[I.Ge2] = -28 * d; },
                (f, s, c, p) => { p[I.X] = 14 * s; p[I.Ha1] = 10 + 8 * s; p[I.Ha2] = -10 + 8 * s; p[I.Ge1] = p[I.Ge2] = -(6 + 14 * Math.Abs(c)); },
                (f, s, c, p) => { p[I.Ha1] = 8 + 62 * Math.Max(0, s); p[I.Ge1] = -8; p[I.Ha2] = -8 + 62 * Math.Max(0, -s); p[I.Ge2] = -8; p[I.Air] = 4 * Math.Abs(s); p[I.Torse] = -6 * Math.Abs(s); },
                (f, s, c, p) => { p[I.Ha1] = 24 + 12 * s; p[I.Ha2] = 4 + 12 * s; p[I.Ge1] = p[I.Ge2] = -32; p[I.Torse] = -8 * s; },
                (f, s, c, p) => { p[I.Ha1] = 38 * s; p[I.Ge1] = -(10 + 70 * Math.Max(0, c)); p[I.Ha2] = -38 * s; p[I.Ge2] = -(10 + 70 * Math.Max(0, -c)); p[I.Air] = 3 * Math.Abs(s); },
                (f, s, c, p) => { double u = Math.Max(0, s), d = Math.Max(0, -s); p[I.Air] = 26 * u * u; p[I.Ha1] = 8 + 10 * u + 17 * d; p[I.Ha2] = -8 - 10 * u + 17 * d; p[I.Ge1] = p[I.Ge2] = -34 * d; },
                (f, s, c, p) => { double d = (1 - c) / 2; p[I.Ha1] = 8 + 62 * d; p[I.Ge1] = -112 * d; p[I.Ha2] = -4 + 56 * d; p[I.Ge2] = -104 * d; p[I.Torse] = 22 * d; },
                (f, s, c, p) => { p[I.Ha1] = 6 + 28 * Math.Max(0, s); p[I.Ge1] = -10 * Math.Max(0, -s); p[I.Ha2] = -6 + 34 * Math.Max(0, -s); p[I.Ge2] = -10 * Math.Max(0, s); },
                // genoux levés tour à tour ; charleston : la même jambe devant puis derrière ; accroupi : les coups de pied du kazatchok
                (f, s, c, p) => { double u = Math.Max(0, s), d = Math.Max(0, -s); p[I.Ha1] = 8 + 78 * u; p[I.Ge1] = -8 - 92 * u; p[I.Ha2] = -8 + 86 * d; p[I.Ge2] = -8 - 92 * d; p[I.Air] = 3 * Math.Abs(s); },
                (f, s, c, p) => { p[I.Ha1] = 46 * s; p[I.Ge1] = -6 - 44 * Math.Max(0, -s); p[I.Ha2] = -6; p[I.Ge2] = -10 - 12 * Math.Abs(s); p[I.Torse] = -4 * s; },
                (f, s, c, p) => { double u = Math.Max(0, s), d = Math.Max(0, -s); p[I.Ha1] = 62 + 33 * u; p[I.Ge1] = -128 + 123 * u; p[I.Ha2] = 58 + 37 * d; p[I.Ge2] = -124 + 119 * d; p[I.Torse] = -6; },
                (f, s, c, p) => { double u = Math.Max(0, s), d = Math.Max(0, -s); p[I.Ha1] = 8 + 40 * u - 30 * d; p[I.Ge1] = -60 * u - 10 * d; p[I.Ha2] = -8 - 30 * u + 40 * d; p[I.Ge2] = -10 * u - 60 * d; },
                (f, s, c, p) => { double u = Math.Max(0, s), d = Math.Max(0, -s); p[I.Ha1] = 6 + 34 * u; p[I.Ge1] = -4; p[I.Ha2] = -6 - 30 * d; p[I.Ge2] = -22 * d; },      // pointes : un pied devant, l'autre derrière
                (f, s, c, p) => { double u = Math.Max(0, s), d = Math.Max(0, -s); p[I.Ha1] = 6 - 36 * u; p[I.Ge1] = -72 * u; p[I.Ha2] = -6 - 30 * d; p[I.Ge2] = -72 * d; p[I.Torse] = 6; },      // ruades : les talons partent en arrière
            };
            for (int i = 0; i < bras.Length; i++)
                for (int j = 0; j < jambes.Length; j++)
                {
                    var b = bras[i];
                    var g = jambes[j];
                    Anim a = Ajouter("Danses", nomsBras[i] + " + " + nomsJambes[j], tempos[j], 6, t =>
                    {
                        double f = 2 * Math.PI * t, s = Math.Sin(f), c = Math.Cos(f);
                        var p = new double[I.N];
                        g(f, s, c, p);
                        b(f, s, c, p);
                        return p;
                    });
                    a.Objet = (i + j) % 5 == 0 ? "note" : null;
                    if (i == 1 && j == 1) Danse = a;
                }
        }

        // ------------------------------------------------------------ gestes (haut du corps)

        static void Gestes()
        {
            const string F = "Gestes";
            Salut = Osc(F, "Salut", "0 -5 150 30 -8 12", "0 -5 150 80 -8 12", 0.5, 4);
            Autre(Salut);
            Autre(Osc(F, "Grand salut", "-4 -8 175 -20 -10 12", "-4 -8 150 40 -10 12", 0.6, 4));
            Autre(Osc(F, "Salut timide", "4 10 70 110 -5 10", "4 10 70 140 -5 10", 0.4, 4));
            Osc(F, "Coucou à deux mains", "0 -6 150 30 160 -30", "0 -6 150 75 160 -75", 0.5, 4);
            Autre(Osc(F, "Pointe devant", "4 0 90 0 -10 12", "6 0 93 4 -10 12", 0.8, 2));
            Autre(Osc(F, "Pointe en haut", "-6 -20 165 0 -10 12", "-6 -22 169 3 -10 12", 0.8, 2));
            Autre(Osc(F, "Pointe derrière", "-4 0 -95 0 10 12", "-4 0 -91 -4 10 12", 0.8, 2));
            Autre(Osc(F, "Poing levé", "0 -8 178 0 -10 15", "0 -8 168 22 -10 15", 0.4, 3));
            Osc(F, "Applaudit", "2 0 45 75 78 45", "2 0 60 62 62 58", 0.3, 6);
            Osc(F, "Se frotte les mains", "8 6 55 70 60 66", "8 6 60 66 55 70", 0.25, 6);
            Osc(F, "Bras croisés", "-2 0 35 115 45 105", "-2 2 36 116 44 104", 1.5, 2);
            Osc(F, "Mains sur les hanches", "-3 -3 -35 105 -45 112", "-3 -5 -36 107 -44 110", 1.5, 2);
            Autre(Osc(F, "Regarde au loin", "6 -6 110 125 -12 14", "8 -6 112 123 -12 14", 1.2, 2));
            Autre(Osc(F, "Facepalm", "8 22 80 135 -8 12", "8 26 80 137 -8 12", 1.2, 2));
            Osc(F, "Hausse les épaules", "0 -6 35 80 25 85", "0 4 15 30 5 35", 0.5, 3);
            Autre(Osc(F, "Viens ici", "2 0 85 20 -8 12", "2 0 85 120 -8 12", 0.5, 3));
            Autre(Osc(F, "Stop", "-3 0 88 8 -8 12", "-3 0 91 5 -8 12", 1.0, 2));
            Autre(Osc(F, "Salut militaire", "0 -2 95 135 -4 4", "0 -2 96 136 -4 4", 1.5, 1));
            Autre(Osc(F, "Envoie un bisou", "4 5 70 140 -8 12", "0 -5 95 10 -8 12", 0.9, 2, "coeur"));
            Autre(Osc(F, "Dab", "20 30 130 -5 120 150", "22 32 132 -5 122 150", 1.0, 2));
            Autre(Osc(F, "Coup de chapeau", "6 10 120 120 -8 12", "0 0 130 60 -8 12", 0.7, 2));
            Autre(Osc(F, "Fait non du doigt", "0 0 80 95 -8 12", "0 0 80 75 -8 12", 0.3, 5));
            Osc(F, "Montre ses muscles", "-4 -5 95 100 -95 -100", "-4 -5 100 115 -100 -115", 0.6, 3);
            Autre(Osc(F, "Se gratte la tête", "4 8 150 105 -8 12", "4 8 150 125 -8 12", 0.25, 6));
            Autre(Osc(F, "Réfléchit", "6 10 55 125 20 95", "6 14 55 127 20 95", 1.5, 2, "question"));
            Autre(Osc(F, "Bâille", "-8 -15 100 130 -8 12", "-10 -20 100 132 -8 12", 1.4, 1));
            Osc(F, "S'étire", "-10 -15 170 15 -170 -15", "-14 -20 178 5 -178 -5", 1.2, 2);
            Autre(Osc(F, "Regarde sa montre", "6 20 60 85 -8 12", "6 22 60 87 -8 12", 1.3, 1));
            Autre(Osc(F, "Téléphone", "0 -5 110 140 -8 12", "0 -3 110 142 -8 12", 1.5, 2, "telephone"));
            Autre(Osc(F, "Yes !", "0 -5 40 130 -8 12", "-4 -8 60 150 -8 12", 0.35, 3));
            Osc(F, "Hoche la tête", "0 14 10 12 -10 12", "0 -8 10 12 -10 12", 0.4, 4);
            Osc(F, "Mains dans le dos", "-3 -3 -30 -60 -35 -55", "-3 -6 -31 -60 -36 -55", 1.6, 2);
            Autre(Osc(F, "Boit", "0 0 60 120 -8 12", "-10 -25 75 135 -8 12", 1.2, 2));
            Autre(Osc(F, "Lit", "8 22 45 95 50 90", "8 24 46 96 51 89", 1.6, 3, "livre"));
            Autre(Osc(F, "Idée !", "0 -10 165 0 -8 12", "0 -12 168 2 -8 12", 0.7, 2, "exclam"));
        }

        // ------------------------------------------------------------ combat

        static Anim Coup(string nom, string arme, string frappe, string objet = null)
        {
            return Cles("Combat", nom, "0:" + G + ";130:" + arme + ";220:" + frappe + ";300:" + frappe + ";520:" + G, objet);
        }

        static void Combat()
        {
            const string F = "Combat";
            const string armePoing = "4 0 -15 120 55 110 10 -18 -16 -12";
            const string armePied = "-6 0 60 95 45 105 75 -115 -8 -12";
            Poing = Coup("Coup de poing", armePoing, "16 0 90 0 50 115 24 -22 -22 -6 0 0 8");
            Autre(Poing);
            PoingHaut = Coup("Coup de poing haut", armePoing, "8 -8 122 0 50 115 22 -18 -20 -6 0 0 6");
            Autre(PoingHaut);
            Autre(Coup("Coup de poing bas", armePoing, "26 6 62 0 50 115 30 -32 -22 -10 0 0 8"));
            Autre(Coup("Crochet", "0 0 -40 60 55 110 10 -18 -16 -12", "14 0 85 65 50 115 22 -20 -20 -6 0 0 6"));
            Autre(Coup("Uppercut", "22 5 10 95 55 110 30 -45 -10 -30", "-6 -12 140 45 50 115 14 -8 -14 -4 6 0 4"));
            Autre(Coup("Coup de coude", armePoing, "12 0 80 150 50 115 20 -20 -18 -6 0 0 8"));
            Coup("Double poing", "-4 0 -20 115 -25 118 10 -18 -16 -12", "20 0 92 0 88 4 26 -24 -24 -6 0 0 10");
            PiedMoyen = Coup("Coup de pied", armePied, "-16 0 55 100 30 110 92 -4 -8 -10");
            Autre(PiedMoyen, " (autre jambe)");
            Autre(Coup("Coup de pied haut", armePied, "-30 -5 40 100 20 110 132 -4 -10 -8"), " (autre jambe)");
            PiedBas = Coup("Coup de pied bas", armePied, "-4 0 60 95 45 105 52 -2 -8 -12");
            Autre(PiedBas, " (autre jambe)");
            Autre(Coup("Coup de genou", armePied, "10 0 80 95 70 100 95 -125 -6 -6 2"), " (autre jambe)");
            Autre(Coup("Coup de pied arrière", "25 0 60 95 50 100 40 -110 -6 -12", "48 -10 70 80 60 90 -78 -2 -4 -14"), " (autre jambe)");
            Coup("Balayage", Cr, "20 0 20 20 -60 -10 85 -6 70 -125");
            Cles(F, "Coup de pied sauté", "0:" + G + ";130:" + Cr + ";280:-25 -5 40 100 20 110 120 -4 20 -90 38;420:-10 0 60 95 45 105 60 -60 10 -50 20;560:" + Cr + ";720:" + G);
            Osc(F, "Garde haute", "6 0 110 80 100 85 14 -15 -14 -10", "8 2 112 78 102 83 14 -18 -14 -12", 0.6, 3);
            Osc(F, "Garde basse", "20 10 50 60 40 70 25 -40 -10 -35", "22 12 52 58 42 68 27 -44 -8 -38", 0.6, 3);
            Osc(F, "Sautille en garde", "8 0 70 100 55 110 14 -15 -14 -10", "8 0 72 98 57 108 14 -22 -14 -18 6", 0.3, 8);
            Cles(F, "Esquive arrière", "0:" + G + ";140:-28 -10 20 60 10 70 20 -8 -14 -30 0 0 -8;320:-28 -10 20 60 10 70 20 -8 -14 -30 0 0 -8;520:" + G);
            Cles(F, "Esquive basse", "0:" + G + ";140:30 10 80 110 70 115 70 -118 55 -108;340:30 10 80 110 70 115 70 -118 55 -108;540:" + G);

            // enchaînements : les coups ci-dessus mis bout à bout
            double[] g = L(G), jab = L("16 0 90 0 50 115 24 -22 -22 -6 0 0 8"), croise = Echange(jab), haut = L("8 -8 122 0 50 115 22 -18 -20 -6 0 0 6"),
                pied = L("-16 0 55 100 30 110 92 -4 -8 -10"), piedHaut = L("-30 -5 40 100 20 110 132 -4 -10 -8"), upper = L("-6 -12 140 45 50 115 14 -8 -14 -4 6 0 4");
            ClesP(F, "Enchaînement de 2 coups", new[] { 0, 0.15, 0.3, 0.45, 0.6, 0.85 }, new[] { g, jab, g, croise, croise, g });
            ClesP(F, "Enchaînement de 3 coups", new[] { 0, 0.15, 0.3, 0.45, 0.6, 0.8, 0.95, 1.2 }, new[] { g, jab, g, croise, g, pied, pied, g });
            ClesP(F, "Enchaînement de 5 coups", new[] { 0, 0.14, 0.28, 0.42, 0.56, 0.72, 0.88, 1.04, 1.2, 1.4, 1.55, 1.85 },
                new[] { g, jab, g, croise, g, haut, g, upper, g, piedHaut, piedHaut, g });
            ClesP(F, "Rafale de coups de poing", new[] { 0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 1.0 }, new[] { g, jab, croise, jab, croise, jab, croise, jab, croise, g });

            // épée
            const string gardeEpee = "6 0 40 80 -20 60 14 -15 -14 -10";
            Fixe(F, "Garde à l'épée", gardeEpee, 2, "epee");
            Cles(F, "Épée : taille haute", "0:" + gardeEpee + ";160:-8 -5 160 20 -30 50 10 -12 -16 -8;260:22 5 35 5 -40 40 26 -24 -22 -6 0 0 8;360:22 5 35 5 -40 40 26 -24 -22 -6 0 0 8;600:" + gardeEpee, "epee");
            Cles(F, "Épée : taille montante", "0:" + gardeEpee + ";160:18 5 -30 10 -30 50 20 -25 -16 -10;260:-6 -8 150 10 -40 40 16 -10 -18 -4 0 0 6;360:-6 -8 150 10 -40 40 16 -10 -18 -4 0 0 6;600:" + gardeEpee, "epee");
            Cles(F, "Épée : estoc", "0:" + gardeEpee + ";150:4 0 10 100 -30 50 10 -15 -16 -10;240:14 0 92 0 -50 30 34 -30 -28 -4 0 0 14;340:14 0 92 0 -50 30 34 -30 -28 -4 0 0 14;580:" + gardeEpee, "epee");
            Ajouter(F, "Épée : moulinet", 0.6, 4, t => { double[] p = L(gardeEpee); p[I.Ep1] = 40 + 360 * t; p[I.Co1] = 10; return p; }).Objet = "epee";
            Osc(F, "Épée : parade", "0 0 70 110 -20 60 14 -15 -14 -10", "-4 0 76 112 -22 60 12 -18 -16 -12", 0.5, 3, "epee");
            Cles(F, "Épée : double taille", "0:" + gardeEpee + ";140:-8 -5 160 20 -30 50 10 -12 -16 -8;230:22 5 35 5 -40 40 26 -24 -22 -6 0 0 8;360:18 5 -30 10 -30 50 20 -25 -16 -10;450:-6 -8 150 10 -40 40 16 -10 -18 -4 0 0 6;560:-6 -8 150 10 -40 40 16 -10 -18 -4;800:" + gardeEpee, "epee");

            // bâton et pouvoirs
            Ajouter(F, "Bâton : tourbillon", 0.7, 5, t => L("-4 -8 150 30 170 -10 16 -12 -16 -8")).Objet = "baton";
            Cles(F, "Bâton : frappe au sol", "0:" + G + ";200:-10 -10 165 10 170 5 14 -10 -16 -6;330:40 15 40 5 45 0 60 -95 30 -70;480:40 15 40 5 45 0 60 -95 30 -70;760:" + G, "baton2");
            Cles(F, "Boule d'énergie", "0:" + G + ";450:-12 0 -35 70 -25 80 20 -30 -18 -25;900:-14 0 -38 72 -28 82 22 -34 -18 -28;1020:18 0 92 2 86 6 28 -26 -26 -6 0 0 10;1500:18 0 92 2 86 6 28 -26 -26 -6 0 0 10;1800:" + G, "boule");
            Cles(F, "Onde de choc", "0:" + S + ";160:" + Cr + ";400:-10 -10 170 10 160 20 30 -60 10 -50 60;600:40 20 30 0 80 60 80 -120 60 -110;1100:40 20 30 0 80 60 80 -120 60 -110;1400:" + S, "onde");
            Osc(F, "Concentre son énergie", "0 -10 -20 100 -30 105 16 -22 -16 -22", "-3 -14 -24 104 -34 108 18 -26 -18 -26", 0.2, 14, "aura");
            Osc(F, "Provocation", "4 0 85 30 55 110 14 -15 -14 -10", "4 0 85 120 55 110 14 -15 -14 -10", 0.45, 4);
            Cles(F, "Salut martial", "0:" + S + ";300:45 15 60 80 55 85 6 0 -6 0;900:45 15 60 80 55 85 6 0 -6 0;1200:" + S);
        }

        // ------------------------------------------------------------ acrobaties

        static void Acrobaties()
        {
            const string F = "Acrobaties";
            const string haut = "-4 -10 172 5 -172 -5 5 0 -5 0";
            Cles(F, "Saut", "0:" + S + ";160:" + Cr + ";300:" + haut + " 30;430:" + haut + " 48;560:8 0 60 30 50 30 30 -50 20 -45 22;700:" + Cr + ";900:" + S);
            Cles(F, "Grand saut", "0:" + S + ";200:" + Cr + ";380:" + haut + " 60;560:" + haut + " 95;740:8 0 60 30 50 30 30 -50 20 -45 50;900:" + Cr + ";1150:" + S);
            Cles(F, "Saut groupé", "0:" + S + ";160:" + Cr + ";300:" + haut + " 30;440:" + Boule + " 58;580:8 0 60 30 50 30 30 -50 20 -45 24;700:" + Cr + ";900:" + S);
            Cles(F, "Saut en étoile", "0:" + S + ";160:" + Cr + ";300:" + haut + " 28;440:0 -5 150 0 -150 0 45 0 -45 0 52;580:8 0 60 30 50 30 30 -50 20 -45 22;700:" + Cr + ";900:" + S);
            Cles(F, "Saut carpé", "0:" + S + ";160:" + Cr + ";300:" + haut + " 30;440:60 10 80 0 78 0 95 0 90 0 55;580:8 0 60 30 50 30 30 -50 20 -45 22;700:" + Cr + ";900:" + S);
            Cles(F, "Salto avant", "0:" + S + ";140:" + Cr + ";260:-5 -10 160 10 -170 -10 8 -5 -4 -5 30 20;400:" + Boule + " 78 150;540:" + Boule + " 72 285;660:5 0 80 20 70 25 40 -40 25 -35 22 350;760:" + Cr + " 0 360;950:" + S + " 0 360", null, 60);
            Cles(F, "Salto arrière", "0:" + S + ";140:" + Cr + ";260:-15 -20 170 0 -175 0 8 -5 -4 -5 30 -25;400:" + Boule + " 78 -150;540:" + Boule + " 72 -285;660:5 0 80 20 70 25 40 -40 25 -35 22 -350;760:" + Cr + " 0 -360;950:" + S + " 0 -360", null, -50);
            Cles(F, "Double salto avant", "0:" + S + ";160:" + Cr + ";300:-5 -10 160 10 -170 -10 8 -5 -4 -5 45 20;460:" + Boule + " 115 200;620:" + Boule + " 125 400;780:" + Boule + " 95 600;900:5 0 80 20 70 25 40 -40 25 -35 30 705;1000:" + Cr + " 0 720;1200:" + S + " 0 720", null, 70);
            Cles(F, "Double salto arrière", "0:" + S + ";160:" + Cr + ";300:-15 -20 170 0 -175 0 8 -5 -4 -5 45 -25;460:" + Boule + " 115 -200;620:" + Boule + " 125 -400;780:" + Boule + " 95 -600;900:5 0 80 20 70 25 40 -40 25 -35 30 -705;1000:" + Cr + " 0 -720;1200:" + S + " 0 -720", null, -60);
            const string etoile = "0 0 170 0 190 0 35 0 -35 0";
            Cles(F, "Roue", "0:-4 -8 170 0 190 0 20 0 -20 0;250:" + etoile + " 0 90;500:" + etoile + " 0 180;750:" + etoile + " 0 270;1000:-4 -8 170 0 190 0 20 0 -20 0 0 360;1200:" + S + " 0 360", null, 110);
            Cles(F, "Roue arrière", "0:-4 -8 170 0 190 0 20 0 -20 0;250:" + etoile + " 0 -90;500:" + etoile + " 0 -180;750:" + etoile + " 0 -270;1000:-4 -8 170 0 190 0 20 0 -20 0 0 -360;1200:" + S + " 0 -360", null, -110);
            const string poirier = "0 0 180 0 178 0 4 0 -4 0 0 180";
            Cles(F, "Poirier", "0:" + S + ";300:80 10 150 0 145 0 30 -40 -30 0 0 40;600:" + poirier + ";1200:0 5 180 0 178 0 14 -10 -14 -10 0 176;1800:" + poirier + ";2100:80 10 150 0 145 0 30 -40 -30 0 0 40;2400:" + S);
            Anim mains = Ajouter(F, "Marche sur les mains", 0.8, 6, t =>
            {
                double s = Math.Sin(2 * Math.PI * t);
                double[] p = L(poirier);
                p[I.Ep1] = 180 + 18 * s; p[I.Ep2] = 180 - 18 * s; p[I.Ha1] = 10 + 14 * s; p[I.Ha2] = -10 - 14 * s; p[I.Ge1] = -12; p[I.Ge2] = -12;
                return p;
            });
            mains.Vitesse = 40;
            Cles(F, "Équilibre sur une main", "0:" + S + ";300:80 10 150 0 145 0 30 -40 -30 0 0 40;600:" + poirier + ";900:0 0 180 0 100 20 20 -30 -25 0 0 180;1900:0 0 180 0 104 22 24 -34 -28 0 0 178;2200:" + poirier + ";2500:80 10 150 0 145 0 30 -40 -30 0 0 40;2800:" + S);
            Cles(F, "Roulade avant", "0:" + S + ";150:" + Cr + ";300:60 40 60 80 55 85 100 -130 95 -125 0 90;450:" + Boule + " 0 180;600:" + Boule + " 0 270;750:" + Cr + " 0 360;950:" + S + " 0 360", null, 110);
            Cles(F, "Roulade arrière", "0:" + S + ";150:" + Cr + ";300:" + Boule + " 0 -90;450:" + Boule + " 0 -180;600:" + Boule + " 0 -270;750:" + Cr + " 0 -360;950:" + S + " 0 -360", null, -110);
            Cles(F, "Saut de mains", "0:" + S + ";150:10 0 170 0 172 0 30 -30 -20 -10;300:0 0 180 0 178 0 30 0 -30 0 0 120;450:" + poirier + ";600:-20 -10 180 0 178 0 20 -10 -10 -10 30 270;750:" + Cr + " 0 360;950:" + S + " 0 360", null, 100);
            Cles(F, "Flip-flap", "0:" + S + ";150:-10 -10 170 0 -170 0 30 -50 20 -45;300:-20 -20 180 0 178 0 20 -10 -10 -10 30 -90;450:0 0 180 0 178 0 4 0 -4 0 0 -180;600:30 10 180 0 178 0 60 0 55 0 25 -270;750:" + Cr + " 0 -360;900:" + S + " 0 -360", null, -90);
            Cles(F, "Kip-up", "0:" + S + ";300:-10 0 -30 -8 -40 -8 120 -60 112 -52;600:" + Dos + ";900:" + Dos + ";1100:0 0 20 10 25 5 100 0 96 0 0 -100;1250:-10 0 -40 20 -50 20 60 -40 50 -35 40 -30;1380:" + Cr + " 10;1480:" + Cr + ";1700:" + S);
            Cles(F, "Grand écart", "0:" + S + ";500:0 0 170 0 -170 0 90 0 -90 0;1700:0 -4 175 0 -175 0 90 0 -90 0;2200:" + S);
            Osc(F, "Toupie sur la tête", "0 0 120 40 -120 -40 40 -20 -40 -20 0 180", "0 0 120 40 -120 -40 -40 -20 40 -20 0 180", 0.4, 8);
            Fixe(F, "Freeze de breakdance", "0 0 160 60 30 100 100 -60 60 -120 0 150", 2);
            Cles(F, "Saut en longueur", "0:" + S + ";160:" + Cr + ";300:10 -5 150 10 -120 -10 60 -40 -30 -20 35;480:30 0 100 10 90 10 95 -20 85 -15 42;640:30 5 60 20 50 20 80 -70 70 -65 12;760:" + Cr + ";960:" + S, null, 220);
            Cles(F, "Plongeon roulé", "0:" + S + ";150:" + Cr + ";300:0 -10 170 0 175 0 -10 0 -16 0 40 70;450:60 40 60 80 55 85 100 -130 95 -125 10 180;600:" + Boule + " 0 270;750:" + Cr + " 0 360;950:" + S + " 0 360", null, 170);
        }

        // ------------------------------------------------------------ sport

        static void Sport()
        {
            const string F = "Sport";
            Osc(F, "Pompes", Planche, "0 -10 25 135 27 135 0 0 2 0 0 80", 1.0, 6);
            Osc(F, "Abdos", "0 0 150 100 150 100 75 -120 70 -115 0 -75", "15 0 100 140 100 140 120 -60 115 -55", 1.4, 5);
            Osc(F, "Squats", "0 0 80 10 80 10 6 0 -6 0", "30 -15 85 5 85 5 75 -120 65 -112", 1.2, 5);
            Autre(Osc(F, "Fentes", S, "5 0 -35 105 -45 112 70 -85 -40 -75", 1.3, 4), " (autre jambe)");
            Osc(F, "Jumping jacks", "0 0 10 5 -10 5 4 0 -4 0", "0 0 170 0 -170 0 25 0 -25 0 10", 0.6, 8);
            Osc(F, "Touche ses pieds", "-5 -10 175 0 -175 0 4 0 -4 0", "95 15 20 0 15 0 4 0 -4 0", 1.8, 3);
            Fixe(F, "Planche", Planche, 3);
            Fixe(F, "Yoga : l'arbre", "0 0 175 8 -175 -8 0 0 55 -135", 3);
            Fixe(F, "Yoga : le guerrier", "0 0 90 0 -90 0 55 -75 -45 0", 3);
            Fixe(F, "Yoga : chien tête en bas", "0 15 180 0 178 0 90 0 86 0 0 120", 3);
            Fixe(F, "Yoga : le cobra", "-50 -10 75 0 78 0 -15 0 -12 0 0 75", 3);
            Fixe(F, "Yoga : la chandelle", "0 30 -150 20 -155 20 0 0 4 0 0 -170", 3);
            Osc(F, "Haltères", "0 0 10 10 8 10 8 0 -8 0", "0 0 20 140 18 140 8 0 -8 0", 1.0, 6, "haltere");
            Osc(F, "Développé au-dessus de la tête", "0 0 60 130 62 130 10 -10 -10 -10", "0 -8 178 0 180 0 8 0 -8 0", 1.1, 5, "haltere");
            Osc(F, "Corde à sauter", "0 0 20 60 -20 -60 6 -5 -6 -5", "0 0 22 62 -22 -62 8 -22 -8 -22 12", 0.4, 12, "corde");
            Cles(F, "Burpee", "0:" + S + ";200:" + Cr + ";400:" + Planche + ";600:0 -10 25 135 27 135 0 0 2 0 0 80;800:" + Planche + ";1000:" + Cr + ";1150:-4 -10 172 5 -172 -5 5 0 -5 0 30;1320:" + Cr + ";1500:" + S);
            Cles(F, "Tir au but", "0:" + S + ";250:-8 0 60 20 -60 10 -50 -80 8 -10;380:-18 0 -40 20 80 10 82 -4 -6 -12 0 0 6;520:-18 0 -40 20 80 10 82 -4 -6 -12 0 0 6;800:" + S, "ballonpied");
            Osc(F, "Dribble", "14 12 30 40 -20 60 16 -22 -12 -20", "16 14 20 10 -20 60 18 -26 -12 -24", 0.4, 10, "ballonmain");
            Cles(F, "Tir au panier", "0:14 12 30 40 -20 60 16 -22 -12 -20;250:20 5 60 110 55 115 50 -90 40 -85;450:-6 -15 165 10 150 30 6 0 -6 0 35;600:-6 -15 170 -30 150 30 6 0 -6 0 30;800:" + Cr + ";1000:" + S, "ballontir");
            Cles(F, "Swing de golf", "0:20 15 35 5 40 0 14 -15 -14 -15;400:15 5 -120 -30 -110 -40 16 -18 -12 -14;560:22 15 35 5 40 0 16 -16 -14 -14;720:-8 -10 150 30 145 35 10 -8 -18 -20;1100:-8 -12 152 32 147 37 10 -8 -18 -20;1400:" + S, "club");
            Cles(F, "Service de tennis", "0:" + S + ";300:-6 -15 -60 80 165 5 10 -12 -10 -12;500:-10 -20 150 60 120 20 12 -20 -10 -20 8;600:25 5 60 0 -30 40 26 -26 -20 -8 0 0 8;800:25 5 30 5 -30 40 26 -26 -20 -8;1100:" + S, "raquette");
            Cles(F, "Lancer", "0:" + S + ";250:-12 -5 -120 60 70 20 -20 -10 20 -15;400:20 5 100 5 -40 30 28 -26 -24 -6 0 0 10;550:25 8 50 5 -40 30 28 -26 -24 -6 0 0 10;850:" + S, "ballontir");
            Cles(F, "Bowling", "0:" + S + ";300:15 5 -70 5 40 30 30 -30 -30 -10;500:35 10 50 0 -50 20 60 -85 -50 -30 0 0 10;750:35 10 110 0 -50 20 60 -85 -50 -30 0 0 10;1100:" + S, "ballonroule");
            Ajouter(F, "Nage", 1.0, 5, t =>
            {
                double f = 2 * Math.PI * t;
                double[] p = L("0 -15 0 10 0 10 0 0 0 0 26 80");
                p[I.Ep1] = 360 * t + 80; p[I.Ep2] = 360 * t + 260; p[I.Ha1] = 12 * Math.Sin(3 * f); p[I.Ha2] = -12 * Math.Sin(3 * f);
                p[I.Air] = 26 + 3 * Math.Sin(f);
                return p;
            });
            Ajouter(F, "Pédale", 0.7, 6, t =>
            {
                double f = 2 * Math.PI * t;
                double[] p = L("-20 5 -30 -8 -40 -8 0 0 0 0");
                p[I.Ha1] = 105 + 28 * Math.Sin(f); p[I.Ge1] = -70 - 40 * Math.Cos(f); p[I.Ha2] = 105 - 28 * Math.Sin(f); p[I.Ge2] = -70 + 40 * Math.Cos(f);
                return p;
            });
            Ajouter(F, "Grimpe", 0.9, 5, t =>
            {
                double s = Math.Sin(2 * Math.PI * t);
                double[] p = L("4 -12 0 0 0 0 0 0 0 0");
                p[I.Ep1] = 150 + 25 * s; p[I.Co1] = 30 - 25 * s; p[I.Ep2] = 150 - 25 * s; p[I.Co2] = 30 + 25 * s;
                p[I.Ha1] = 45 - 40 * s; p[I.Ge1] = -70 + 55 * s; p[I.Ha2] = 45 + 40 * s; p[I.Ge2] = -70 - 55 * s;
                p[I.Air] = 14 + 3 * Math.Abs(s);
                return p;
            });
            Osc(F, "Skateboard", "18 0 40 10 -60 -10 40 -70 20 -60", "22 4 55 15 -45 -5 46 -80 24 -68", 0.9, 4, "planche");
            Osc(F, "Surf", "14 -5 80 5 -85 -5 50 -80 -10 -45", "20 0 70 10 -95 0 56 -90 -6 -52", 1.1, 4, "planche");
        }

        // ------------------------------------------------------------ vie quotidienne

        static void Quotidien()
        {
            const string F = "Quotidien";
            Osc(F, "S'assoit", "-10 0 -30 -8 -40 -8 120 -60 112 -52", "-12 4 -30 -8 -40 -8 120 -60 112 -52", 2, 3);
            Osc(F, "Assis jambes tendues", "-15 0 -35 -5 -45 -5 90 0 84 0", "-15 5 -35 -5 -45 -5 90 0 84 0", 2, 3);
            Osc(F, "Assis, balance les pieds", "-10 0 -30 -8 -40 -8 100 -20 96 -70", "-10 2 -30 -8 -40 -8 96 -70 100 -20", 0.8, 6);
            Osc(F, "Méditation", "0 0 60 60 55 65 100 -160 95 -155 12", "0 2 60 60 55 65 100 -160 95 -155 20", 2.4, 4, "aura");
            Fixe(F, "À genoux", "0 0 5 10 -5 10 5 -95 -5 -90", 2);
            Dort = Osc(F, "Dort", Dos, "3 12 25 5 28 -5 15 0 12 0 0 -75", 3, 4, "zzz");
            Osc(F, "Sieste sur le ventre", Ventre, "0 3 165 20 170 10 -15 0 -12 0 0 73", 3, 4, "zzz");
            Osc(F, "Allongé, bras sous la tête", "0 -10 -150 120 -145 125 15 0 60 -100 0 -75", "2 -8 -150 120 -145 125 15 0 64 -104 0 -75", 2.5, 3);
            Osc(F, "Tape au clavier", "5 15 55 45 60 40 90 0 84 0", "5 15 58 42 57 43 90 0 84 0", 0.2, 16, "laptop");
            Ajouter(F, "Dessine", 2.0, 3, t =>
            {
                double f = 2 * Math.PI * t;
                double[] p = L("4 -5 0 0 -10 12 8 0 -8 0");
                p[I.Ep1] = 95 + 28 * Math.Sin(f); p[I.Co1] = 25 + 25 * Math.Cos(2 * f);
                return p;
            }).Objet = "crayon";
            Cles(F, "Éternue", "0:" + S + ";500:-15 -25 60 100 -8 12 6 0 -6 0;650:35 40 70 120 -8 20 14 -18 -4 -14;900:30 30 70 120 -8 20 12 -14 -4 -10;1300:" + S, "exclam");
            Osc(F, "Frissonne", "10 10 40 115 50 105 12 -15 -2 -15", "12 12 44 112 46 108 14 -18 0 -18", 0.12, 16);
            Osc(F, "Rit", "-12 -20 -30 100 -40 108 6 0 -6 0", "8 5 -30 100 -40 108 8 -10 -4 -10", 0.25, 8);
            Osc(F, "Soupire", S, "10 20 5 5 -5 5 6 0 -6 0", 1.6, 2);
            Osc(F, "Tape du pied", "0 0 35 115 45 105 6 0 -6 0", "0 0 35 115 45 105 24 -4 -6 0", 0.35, 8);
            Osc(F, "Siffle", "-4 -10 -30 -40 -35 -45 6 0 -6 0", "-4 -4 -30 -40 -35 -45 6 0 -6 0", 0.6, 5, "note");
            Cles(F, "S'incline", "0:" + S + ";350:70 20 -10 5 -15 5 6 0 -6 0;900:70 20 -10 5 -15 5 6 0 -6 0;1300:" + S);
            Cles(F, "Ramasse quelque chose", "0:" + S + ";350:60 20 40 0 10 10 60 -100 40 -90;700:62 22 44 0 10 10 62 -104 40 -92;1100:" + S);
            Osc(F, "Reprend son souffle", "45 15 28 0 22 0 14 -12 -4 -12", "50 18 28 0 22 0 14 -14 -4 -14", 0.5, 6);
            Osc(F, "Se balance", "-6 0 10 12 -10 12 10 0 -2 0", "6 0 10 12 -10 12 2 0 -10 0", 1.2, 4);
            Osc(F, "Sur la pointe des pieds", S, "-2 -8 4 8 -4 8 3 0 -3 0 6", 0.9, 4);
            Osc(F, "Regarde ses pieds", "14 30 10 12 -10 12 6 0 -6 0", "16 34 10 12 -10 12 12 -4 -6 0", 1.2, 3);
            Osc(F, "Regarde le ciel", "-10 -30 -30 -50 -35 -45 6 0 -6 0", "-12 -34 -30 -50 -35 -45 6 0 -6 0", 1.6, 3);
            Cles(F, "Trébuche", "0:" + S + ";150:25 10 80 20 60 30 40 -60 -30 -10 0 0 6;300:45 15 110 10 100 20 60 -20 -50 -30 0 20 12;480:20 5 40 40 -60 20 30 -50 -10 -30 0 0 16;800:" + S);
            Cles(F, "Glisse et tombe", "0:" + S + ";150:-20 -10 120 20 -100 -20 70 -10 10 -30 6 -20;350:" + Dos + ";1200:" + Dos + ";1500:-10 0 -30 -8 -40 -8 120 -60 112 -52;1800:" + Cr + ";2100:" + S, "exclam");
            Osc(F, "Fait la planche contre un mur", "-14 -6 -30 -30 -35 -25 24 0 14 0", "-16 -4 -30 -30 -35 -25 24 0 14 0", 2, 3);
        }

        // ------------------------------------------------------------ émotions

        static void Emotions()
        {
            const string F = "Émotions";
            Osc(F, "Joie", "0 -10 170 10 -170 -10 6 -5 -6 -5", "0 -15 175 0 -175 0 15 -30 -15 -30 28", 0.45, 5, "note");
            Osc(F, "Victoire", "-6 -15 160 -10 -160 10 8 0 -8 0", "-8 -18 165 -5 -165 5 8 0 -8 0 4", 0.6, 3);
            Fixe(F, "Fier", "-10 -12 -35 105 -45 112 8 0 -8 0", 3);
            Osc(F, "Colère", "8 5 -15 30 -25 30 6 0 -6 0", "10 8 -10 40 -20 40 40 -70 -6 0", 0.3, 6, "exclam");
            Osc(F, "Rage", "-5 -15 150 60 -150 -60 8 -5 -8 -5", "-5 -15 160 30 -160 -30 8 -14 -8 -14 6", 0.2, 8, "exclam");
            Osc(F, "Peur", "25 20 80 130 85 125 30 -50 20 -45", "27 22 84 127 81 128 32 -54 22 -48", 0.1, 20);
            Cles(F, "Surprise", "0:" + S + ";120:-15 -15 120 40 -120 -40 20 -10 -20 -10 14 0 -10;400:-12 -12 110 50 -110 -50 16 -8 -16 -8 0 0 -10;800:" + S, "exclam");
            Osc(F, "Tristesse", "18 30 5 5 -5 5 6 -5 -6 -5", "20 34 5 5 -5 5 6 -6 -6 -6", 1.5, 2);
            Osc(F, "Pleure", "25 35 95 140 100 135 6 -5 -6 -5", "28 38 95 142 100 137 6 -8 -6 -8", 0.5, 6);
            Osc(F, "Désespoir", "25 30 10 5 0 5 5 -95 -5 -90", "28 34 10 5 0 5 5 -95 -5 -90", 1.4, 3);
            Osc(F, "Amour", "0 5 50 120 55 115 6 0 -6 0", "-3 0 50 122 55 117 6 0 -6 0 3", 0.8, 4, "coeur");
            Osc(F, "Confusion", "4 8 150 105 -30 100 6 0 -6 0", "4 -6 150 125 -30 100 6 0 -6 0", 0.9, 3, "question");
            Osc(F, "Timide", "5 15 -30 -50 -35 -45 6 0 20 -40", "5 18 -30 -50 -35 -45 6 0 8 -30", 0.6, 4);
            Fixe(F, "Boude", "-5 -15 35 115 45 105 6 0 -6 0", 3);
            Osc(F, "Impatient", "0 0 -35 105 -45 112 6 0 -6 0", "0 4 -35 105 -45 112 22 -4 -6 0", 0.3, 8);
            Osc(F, "Danse de la victoire", "-6 -10 150 60 -30 100 20 -20 -10 -10", "6 -10 -30 100 150 -60 -10 -10 20 -20 4", 0.5, 6, "note");
            Osc(F, "Ennui", "10 22 60 120 -10 12 10 -4 -8 0", "12 26 60 122 -10 12 10 -4 -8 0", 2.0, 2);
        }
    }
}
