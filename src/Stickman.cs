// Stickman : un bonhomme-bâton qui vit sur le bureau, comme dans les animations où un stick figure s'échappe de son logiciel de dessin.
// Contrairement aux autres mascottes (images toutes faites), il est dessiné par le programme à partir
// d'un squelette : c'est ce qui permet de choisir sa couleur et d'avoir des centaines d'animations
// (voir StickmanAnimations.cs). Compilation : construire.ps1.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace MascotteStickman
{
    static class Programme
    {
        public static readonly string Dossier = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MascotteStickman");

        [STAThread]
        static int Main(string[] args)
        {
            Directory.CreateDirectory(Dossier);
            // MascotteStickman.exe --jouer "Salto avant" : fait jouer une animation à la mascotte déjà lancée
            if (args.Length >= 2 && args[0] == "--jouer")
            {
                File.WriteAllText(Path.Combine(Dossier, "commande.txt"), string.Join(" ", args, 1, args.Length - 1));
                return 0;
            }
            Biblio.Construire();
            R.Familles();
            // MascotteStickman.exe --planches dossier [filtre] : planches de contrôle des animations, en PNG
            if (args.Length >= 2 && args[0] == "--planches")
            {
                Planches.Ecrire(args[1], args.Length > 2 ? args[2] : "");
                return 0;
            }
            if (args.Length >= 2 && args[0] == "--sons") { Sons.Exporter(args[1]); return 0; }
            // MascotteStickman.exe --web dossier : les animations en poses échantillonnées, pour la version web (docs/)
            if (args.Length >= 2 && args[0] == "--web") { Planches.ExporterWeb(args[1]); return 0; }

            bool premiere;
            using (new Mutex(true, "MascotteStickman-Instance", out premiere))
            {
                if (!premiere) return 0;
                var app = new Application();
                app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                app.DispatcherUnhandledException += (s, e) =>
                {
                    try { File.AppendAllText(Path.Combine(Dossier, "erreurs.log"), DateTime.Now + " " + e.Exception + "\r\n"); }
                    catch (IOException) { }
                    e.Handled = true;
                };
                app.Run(new Bonhomme(0));
            }
            return 0;
        }
    }

    // ============================================================ réglages

    sealed class Param
    {
        public string Cle, Nom, Cat;
        public char Type;                        // b = case à cocher, n = nombre, c = choix, k = couleur
        public double V, Defaut, Min, Max;
        public string[] Choix;
    }

    static class R
    {
        public static readonly List<Param> Tous = new List<Param>();
        public static readonly HashSet<string> Coupees = new HashSet<string>();     // animations décochées
        public static bool Sale;
        static readonly Dictionary<string, Param> index = new Dictionary<string, Param>();
        static readonly Dictionary<string, double> lus = new Dictionary<string, double>();
        static string Fichier { get { return Path.Combine(Programme.Dossier, "reglages.txt"); } }

        static R()
        {
            if (File.Exists(Fichier))
                foreach (string ligne in File.ReadAllLines(Fichier))
                {
                    int egal = ligne.IndexOf('=');
                    if (egal <= 0) continue;
                    string cle = ligne.Substring(0, egal), valeur = ligne.Substring(egal + 1);
                    double nombre;
                    if (cle == "sans") Coupees.Add(valeur);
                    else if (double.TryParse(valeur, NumberStyles.Float, CultureInfo.InvariantCulture, out nombre)) lus[cle] = nombre;
                }

            const string A = "Apparence", M = "Mouvement", C = "Comportement", S = "Souris", P = "Physique", Y = "Système";
            K(A, "couleur", "Couleur", 0xFF8A1A);
            B(A, "arcenciel", "Arc-en-ciel (change de couleur en continu)", false);
            B(A, "texturesMinecraft", "Vraies textures de Minecraft (lues dans le jeu installé sur ce PC)", true);
            N(A, "arcVitesse", "Vitesse de l'arc-en-ciel", 60, 5, 300);
            N(A, "taille", "Taille", 1, 0.4, 3);
            N(A, "epaisseur", "Épaisseur du trait", 6, 2, 16);
            N(A, "tete", "Taille de la tête", 13, 6, 26);
            Ch(A, "styleTete", "Tête", 0, "selon la couleur, comme dans la série", "toujours pleine", "toujours creuse (anneau)");
            N(A, "torse", "Longueur du torse", 38, 18, 70);
            N(A, "bras", "Longueur des bras", 19, 8, 36);
            N(A, "jambes", "Longueur des jambes", 22, 10, 40);
            N(A, "contour", "Épaisseur du contour", 1.5, 0, 6);
            K(A, "contourCouleur", "Couleur du contour", 0x1A1A1A);
            N(A, "opacite", "Opacité (%)", 100, 15, 100);
            B(A, "ombre", "Ombre au sol", true);
            B(A, "lueur", "Halo lumineux", false);
            B(A, "trainee", "Traînée derrière la main", false);
            N(A, "traineeLongueur", "Longueur de la traînée", 14, 3, 60);
            B(A, "objets", "Accessoires (épée, ballon, notes…)", true);
            B(A, "bulles", "Bulles de texte", true);

            N(M, "vitesse", "Vitesse des animations (%)", 100, 20, 300);
            N(M, "marche", "Vitesse de déplacement (%)", 100, 20, 400);
            N(M, "fondu", "Douceur des transitions (ms)", 220, 0, 800);
            N(M, "respiration", "Respiration au repos (%)", 100, 0, 300);
            N(M, "fluidite", "Images par seconde", 60, 15, 60);
            N(M, "saut", "Puissance des sauts (%)", 100, 40, 250);

            N(C, "activite", "Secondes entre deux actions", 5, 0.5, 60);
            N(C, "tours", "Durée des animations (%)", 100, 30, 400);
            B(C, "balade", "Se promène", true);
            N(C, "distance", "Distance des promenades", 450, 60, 2500);
            B(C, "partout", "Explore tout l'écran (sinon reste près de chez lui)", true);
            B(C, "fenetres", "Saute sur le haut des fenêtres", true);
            N(C, "fenetresChance", "Envie de sauter sur une fenêtre (%)", 15, 0, 100);
            N(C, "enchaine", "Gestes pendant la marche (%)", 25, 0, 100);
            N(C, "sommeil", "S'endort après (minutes sans toucher au PC, 0 = jamais)", 10, 0, 120);
            N(C, "teleporte", "Envie de se téléporter (%)", 6, 0, 100);
            B(C, "ferme", "MODE FARCEUR : il ferme des fenêtres en appuyant sur leur croix", false);
            N(C, "fermeDelai", "Mode farceur : minutes entre deux fermetures", 5, 0.5, 120);
            B(C, "fermeActive", "Mode farceur : peut aussi fermer la fenêtre que j'utilise", false);
            Ch(C, "styleSaut", "Sauts vers les fenêtres", 0, "variés", "simples", "toujours en salto", "atterrissage de héros");
            B(C, "musique", "Danse en rythme quand le PC joue de la musique (de temps en temps)", true);
            N(C, "musiqueChance", "Envie de danser quand il y a de la musique (%)", 40, 0, 100);

            // plusieurs stickmen : le premier garde la couleur de l'onglet Apparence, les suivants ont la leur
            // (par défaut la bande de la série : rouge, bleu, vert, jaune)
            const string Am = "Amis";
            Ch(Am, "nombre", "Nombre de stickmen", 0, "1", "2", "3", "4", "5", "6");
            B(Am, "rencontres", "Ils vont se voir : bonjour, checks, câlins, duels amicaux…", true);
            N(Am, "rencontresChance", "Envie d'aller voir un ami (%)", 30, 0, 100);
            K(Am, "couleur2", "Couleur du 2e", 0xE53935);
            K(Am, "couleur3", "Couleur du 3e", 0x1E88E5);
            K(Am, "couleur4", "Couleur du 4e", 0x43A047);
            K(Am, "couleur5", "Couleur du 5e", 0xFDD835);
            K(Am, "couleur6", "Couleur du 6e", 0x8E24AA);

            const string Z = "Sons";
            B(Z, "sons", "Sons activés", true);
            N(Z, "volume", "Volume (%)", 40, 0, 100);
            B(Z, "sonsSauts", "Sauts, atterrissages, rebonds et chutes", true);
            B(Z, "sonsSouris", "Quand on l'attrape, le lance ou le bouscule", true);
            B(Z, "sonsAnimations", "Bruitages des animations (coups, épée, énergie, saltos…)", true);
            B(Z, "sonsPouvoirs", "Téléportation", true);

            B(S, "regarde", "Suit le curseur du regard", true);
            B(S, "combat", "Se bat avec le curseur quand il s'approche", true);
            N(S, "portee", "Distance de combat", 130, 40, 500);
            N(S, "combatRepos", "Pause entre deux coups (s)", 2, 0.2, 20);
            B(S, "suit", "Suit le curseur", false);
            B(S, "fuit", "Fuit le curseur", false);
            B(S, "bouscule", "Se fait renverser par un curseur rapide", true);
            B(S, "attrape", "On peut l'attraper à la souris", true);
            B(S, "lancer", "On peut le lancer", true);
            Ch(S, "clic", "Un clic le fait", 4, "saluer", "sauter", "danser", "se battre", "une animation au hasard");

            N(P, "gravite", "Gravité (%)", 100, 10, 400);
            N(P, "rebond", "Rebond (%)", 40, 0, 95);
            N(P, "frottement", "Frottement au sol (%)", 40, 0, 100);
            N(P, "balancier", "Balancement quand on le porte (%)", 100, 0, 300);
            N(P, "force", "Force du lancer (%)", 100, 10, 300);
            N(P, "tournoie", "Vrilles en l'air (%)", 100, 0, 400);
            B(P, "murs", "Rebondit sur les bords de l'écran", true);

            B(Y, "premierplan", "Toujours au premier plan", true);
            N("", "droite", "", 1120, -100000, 100000);
        }

        static Param Ajouter(string cat, string cle, string nom, char type, double defaut, double min, double max)
        {
            var p = new Param { Cat = cat, Cle = cle, Nom = nom, Type = type, Defaut = defaut, Min = min, Max = max };
            double lu;
            p.V = lus.TryGetValue(cle, out lu) ? Math.Max(min, Math.Min(max, lu)) : defaut;
            Tous.Add(p);
            index[cle] = p;
            return p;
        }

        static void N(string cat, string cle, string nom, double defaut, double min, double max) { Ajouter(cat, cle, nom, 'n', defaut, min, max); }
        static void B(string cat, string cle, string nom, bool defaut) { Ajouter(cat, cle, nom, 'b', defaut ? 1 : 0, 0, 1); }
        static void K(string cat, string cle, string nom, int rvb) { Ajouter(cat, cle, nom, 'k', rvb, 0, 0xFFFFFF); }
        static void Ch(string cat, string cle, string nom, int defaut, params string[] choix) { Ajouter(cat, cle, nom, 'c', defaut, 0, choix.Length - 1).Choix = choix; }

        // Une fois les animations construites : un curseur de fréquence par famille.
        public static void Familles()
        {
            foreach (string famille in Biblio.Familles)
                N("Familles", "poids." + famille, famille, famille == "Danses" || famille == "Combat" ? 30 : 50, 0, 100);
        }

        public static double D(string cle) { return index[cle].V; }
        public static bool O(string cle) { return index[cle].V != 0; }
        public static Color Couleur(string cle) { return Rvb((int)index[cle].V); }
        public static Color Rvb(int v) { return Color.FromRgb((byte)(v >> 16), (byte)(v >> 8), (byte)v); }
        public static void Mettre(string cle, double valeur) { index[cle].V = valeur; Sale = true; }

        public static void Enregistrer()
        {
            Sale = false;
            var lignes = new List<string>();
            foreach (Param p in Tous) lignes.Add(p.Cle + "=" + p.V.ToString("0.###", CultureInfo.InvariantCulture));
            foreach (string nom in Coupees) lignes.Add("sans=" + nom);
            try { File.WriteAllLines(Fichier, lignes); }
            catch (IOException) { }
        }
    }

    // ============================================================ squelette et dessin

    static class Dessin
    {
        public struct Os
        {
            public Point Hanche, Cou, Tete, C1, M1, C2, M2, G1, P1, G2, P2;
            public double Rayon;
        }

        static Vector Bas(double degres) { double r = degres * Math.PI / 180; return new Vector(Math.Sin(r), Math.Cos(r)); }
        static Vector Haut(double degres) { double r = degres * Math.PI / 180; return new Vector(Math.Sin(r), -Math.Cos(r)); }

        // Positions des articulations, en pixels à taille 1, personnage tourné vers la droite, y vers le bas.
        // Posé : le point le plus bas touche le sol (y = 0), quelle que soit la pose — poirier, roue, allongé…
        // Suspendu : c'est la tête qui est à l'origine (tenu par le curseur).
        public static Os Calculer(double[] p, bool suspendu)
        {
            double torse = R.D("torse"), bras = R.D("bras"), jambes = R.D("jambes"), tete = R.D("tete");
            double rot = p[I.Rot], dos = rot + p[I.Torse];
            var o = new Os { Rayon = tete };
            o.Cou = o.Hanche + Haut(dos) * torse;
            o.Tete = o.Cou + Haut(dos + p[I.Tete]) * (tete + 1);
            double a = p[I.Ep1] - rot; o.C1 = o.Cou + Bas(a) * bras; o.M1 = o.C1 + Bas(a + p[I.Co1]) * bras;
            a = p[I.Ep2] - rot; o.C2 = o.Cou + Bas(a) * bras; o.M2 = o.C2 + Bas(a + p[I.Co2]) * bras;
            a = p[I.Ha1] - rot; o.G1 = o.Hanche + Bas(a) * jambes; o.P1 = o.G1 + Bas(a + p[I.Ge1]) * jambes;
            a = p[I.Ha2] - rot; o.G2 = o.Hanche + Bas(a) * jambes; o.P2 = o.G2 + Bas(a + p[I.Ge2]) * jambes;

            Vector d;
            if (suspendu) d = new Vector(-o.Tete.X, -o.Tete.Y);
            else
            {
                double bas = Math.Max(o.Tete.Y + tete, 0);
                foreach (Point point in new[] { o.Cou, o.C1, o.M1, o.C2, o.M2, o.G1, o.P1, o.G2, o.P2 }) bas = Math.Max(bas, point.Y);
                d = new Vector(p[I.X], -bas - p[I.Air]);
            }
            o.Hanche += d; o.Cou += d; o.Tete += d; o.C1 += d; o.M1 += d; o.C2 += d; o.M2 += d; o.G1 += d; o.P1 += d; o.G2 += d; o.P2 += d;
            return o;
        }

        public static Point[] Points(Os o) { return new[] { o.Hanche, o.Cou, o.Tete, o.C1, o.M1, o.C2, o.M2, o.G1, o.P1, o.G2, o.P2 }; }

        public static Pen Plume(Color couleur, double epaisseur)
        {
            return new Pen(new SolidColorBrush(couleur), epaisseur) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        }

        // Tout le corps d'abord en contour épais, puis en couleur : le contour entoure la silhouette entière.
        public static void Tracer(DrawingContext dc, Os o, Func<Point, Point> e, double s, Pen contour, Pen trait, Brush plein)
        {
            for (int passe = contour == null ? 1 : 0; passe < 2; passe++)
            {
                Pen plume = passe == 0 ? contour : trait;
                dc.DrawLine(plume, e(o.Cou), e(o.C2)); dc.DrawLine(plume, e(o.C2), e(o.M2));
                dc.DrawLine(plume, e(o.Hanche), e(o.G2)); dc.DrawLine(plume, e(o.G2), e(o.P2));
                dc.DrawLine(plume, e(o.Hanche), e(o.Cou));
                dc.DrawEllipse(passe == 1 ? plein : null, plume, e(o.Tete), o.Rayon * s, o.Rayon * s);
                dc.DrawLine(plume, e(o.Cou), e(o.C1)); dc.DrawLine(plume, e(o.C1), e(o.M1));
                dc.DrawLine(plume, e(o.Hanche), e(o.G1)); dc.DrawLine(plume, e(o.G1), e(o.P1));
            }
        }

        public static FormattedText Texte(string texte, double taille, Brush pinceau, bool gras = false)
        {
            return new FormattedText(texte, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI, Segoe UI Symbol"), FontStyles.Normal, gras ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal),
                taille, pinceau);
        }

        // Une texture du jeu (16 × 16) centrée sur un point du squelette, tournée de `angle` degrés ; faux si elle manque.
        static bool Icone(DrawingContext dc, BitmapSource image, Point centre, double taille, double angle, Func<Point, Point> e, double s)
        {
            if (image == null) return false;
            Point c = e(centre);
            double sens = e(new Point(1, 0)).X >= e(new Point(0, 0)).X ? 1 : -1, t = taille * s;
            dc.PushTransform(new TranslateTransform(c.X, c.Y));
            dc.PushTransform(new ScaleTransform(sens, 1));                   // tourné vers la gauche : l'objet aussi
            dc.PushTransform(new RotateTransform(angle));
            dc.DrawImage(image, new Rect(-t / 2, -t / 2, t, t));
            dc.Pop(); dc.Pop(); dc.Pop();
            return true;
        }

        // Un outil tenu en main : dans le jeu son manche part du coin bas-gauche, on l'aligne sur l'avant-bras.
        static bool Outil(DrawingContext dc, string nom, Os o, Vector avantBras, double taille, Func<Point, Point> e, double s)
        {
            return Icone(dc, Textures.Lire(nom), o.M1 + avantBras * (taille * 0.32), taille, Math.Atan2(avantBras.Y, avantBras.X) * 180 / Math.PI + 45, e, s);
        }

        // Un bloc posé devant lui : x = son bord gauche dans le repère du squelette, bas = hauteur de sa base.
        static void Bloc(DrawingContext dc, int bloc, double x, double bas, Func<Point, Point> e, Color couleur, int graine = 0)
        {
            Blocs.Dessiner(dc, new Rect(e(new Point(x, -bas - Biblio.Bloc)), e(new Point(x + Biblio.Bloc, -bas))), bloc, couleur, graine);
        }

        // Accessoires : quelques traits et ronds posés par-dessus le squelette, selon l'animation.
        public static void Objet(DrawingContext dc, string objet, Os o, Func<Point, Point> e, double s, double phase, Color couleur, double epaisseur)
        {
            Pen gris = Plume(Color.FromRgb(0xE8, 0xE8, 0xEE), Math.Max(2, epaisseur * 0.6));
            Pen fin = Plume(Color.FromRgb(0xE8, 0xE8, 0xEE), Math.Max(1.5, epaisseur * 0.35));
            Brush clair = new SolidColorBrush(Color.FromRgb(0xF4, 0xF4, 0xF8));
            Brush teinte = new SolidColorBrush(couleur);
            Vector avantBras = o.M1 - o.C1;
            if (avantBras.Length > 0.01) avantBras.Normalize();
            Point tete = e(o.Tete);
            double f = 2 * Math.PI * phase;
            switch (objet)
            {
                case "epee": case "club": case "raquette":
                    {
                        double longueur = objet == "epee" ? 48 : objet == "club" ? 52 : 30;
                        Point bout = o.M1 + avantBras * longueur;
                        dc.DrawLine(gris, e(o.M1), e(bout));
                        Vector travers = new Vector(-avantBras.Y, avantBras.X);
                        if (objet == "epee") dc.DrawLine(gris, e(o.M1 + avantBras * 6 + travers * 6), e(o.M1 + avantBras * 6 - travers * 6));
                        else if (objet == "club") dc.DrawLine(gris, e(bout), e(bout + travers * 8));
                        else dc.DrawEllipse(null, fin, e(bout + avantBras * 10), 9 * s, 12 * s);
                        break;
                    }
                case "baton":
                    {
                        Vector axe = new Vector(Math.Cos(2 * f), Math.Sin(2 * f)) * 44;
                        dc.DrawLine(gris, e(o.M1 + axe), e(o.M1 - axe));
                        break;
                    }
                case "baton2": case "haltere":
                    {
                        Vector axe = o.M1 - o.M2;
                        if (axe.Length < 4) axe = new Vector(1, 0);
                        axe.Normalize();
                        double depasse = objet == "haltere" ? 12 : 34;
                        Point a = o.M1 + axe * depasse, b = o.M2 - axe * depasse;
                        dc.DrawLine(gris, e(a), e(b));
                        if (objet == "haltere") { dc.DrawEllipse(clair, null, e(a), 7 * s, 7 * s); dc.DrawEllipse(clair, null, e(b), 7 * s, 7 * s); }
                        break;
                    }
                case "corde":
                    {
                        Point a = e(o.M1), b = e(o.M2), milieu = new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2 + 95 * s * Math.Cos(f));
                        var arc = new StreamGeometry();
                        using (StreamGeometryContext g = arc.Open()) { g.BeginFigure(a, false, false); g.QuadraticBezierTo(milieu, b, true, true); }
                        dc.DrawGeometry(null, fin, arc);
                        break;
                    }
                case "ballonpied": case "ballontir": case "ballonroule": case "ballonmain":
                    {
                        Point balle;
                        if (objet == "ballonmain") balle = new Point(o.M1.X + 4, o.M1.Y + 8 + (-o.M1.Y - 16) * Math.Abs(Math.Sin(f)));
                        else if (objet == "ballonpied") { double k = Math.Max(0, phase - 0.42); balle = new Point(o.P1.X + 8 + 520 * k, -7 - 300 * k + 420 * k * k); if (phase < 0.42) balle = new Point(26, -7); }
                        else if (objet == "ballonroule") { double k = Math.Max(0, phase - 0.6); balle = phase < 0.6 ? o.M1 + new Vector(0, 8) : new Point(o.M1.X + 430 * k, -7); }
                        else { double k = Math.Max(0, phase - 0.5); balle = phase < 0.5 ? o.M1 + new Vector(2, -8) : new Point(o.M1.X + 330 * k, o.M1.Y - 8 - 420 * k + 900 * k * k); }
                        dc.DrawEllipse(clair, fin, e(balle), 7 * s, 7 * s);
                        break;
                    }
                case "planche":
                    {
                        Point a = new Point(Math.Min(o.P1.X, o.P2.X) - 12, 0), b = new Point(Math.Max(o.P1.X, o.P2.X) + 12, 0);
                        dc.DrawLine(gris, e(a), e(b));
                        break;
                    }
                case "laptop":
                    {
                        Point a = o.G1 + new Vector(-16, -5), b = o.G1 + new Vector(8, -5);
                        dc.DrawLine(gris, e(a), e(b));
                        dc.DrawLine(gris, e(b), e(b + new Vector(8, -18)));
                        break;
                    }
                case "livre": case "telephone":
                    {
                        Point m = e(o.M1);
                        double l = (objet == "livre" ? 16 : 6) * s, h = (objet == "livre" ? 11 : 11) * s;
                        dc.DrawRectangle(clair, fin, new Rect(m.X - l / 2, m.Y - h, l, h));
                        break;
                    }
                case "crayon":
                    dc.DrawLine(Plume(Color.FromRgb(0xFF, 0xD5, 0x4F), Math.Max(2, epaisseur * 0.5)), e(o.M1), e(o.M1 + avantBras * 14));
                    break;
                case "elytres":
                    {
                        // deux ailes grises déployées dans le dos, de l'épaule vers les pieds
                        Vector u = o.Hanche - o.Cou;
                        if (u.Length > 0.01) u.Normalize();
                        var n = new Vector(-u.Y, u.X);
                        Func<double, double, Point> p = (a, b) => e(o.Cou + u * a + n * b);
                        Action<Color, Point[]> aile = (c, pts) =>
                        {
                            var g = new StreamGeometry();
                            using (StreamGeometryContext k = g.Open()) { k.BeginFigure(pts[0], true, true); k.PolyLineTo(pts.Skip(1).ToList(), true, true); }
                            dc.DrawGeometry(new SolidColorBrush(c), Plume(Color.FromRgb(0x4E, 0x4E, 0x6A), Math.Max(1, epaisseur * 0.25)), g);
                        };
                        aile(Color.FromRgb(0x77, 0x77, 0x9A), new[] { p(2, 2), p(54, 30), p(46, 3) });
                        aile(Color.FromRgb(0x9A, 0x9A, 0xB8), new[] { p(0, 3), p(62, 42), p(52, 7), p(28, 5) });
                        break;
                    }
                case "epeediamant":
                    if (!Outil(dc, "item/diamond_sword", o, avantBras, 46, e, s))
                        dc.DrawLine(Plume(Color.FromRgb(0x4A, 0xED, 0xD9), Math.Max(3, epaisseur * 0.7)), e(o.M1), e(o.M1 + avantBras * 40));
                    break;
                case "seau":
                    if (!Icone(dc, Textures.Lire("item/water_bucket"), o.M1 + new Vector(2, 8), 30, 0, e, s))
                        dc.DrawRectangle(Blocs.Pinceau(Color.FromRgb(0x3F, 0x76, 0xE4)), gris, new Rect(e(o.M1 + new Vector(-8, 0)), e(o.M1 + new Vector(8, 16))));
                    break;
                case "pomme":
                    if (!Icone(dc, Textures.Lire("item/golden_apple"), o.M1 + new Vector(3, -3), 22, 0, e, s))
                        dc.DrawEllipse(Blocs.Pinceau(Color.FromRgb(0xF2, 0xD4, 0x40)), null, e(o.M1), 7 * s, 7 * s);
                    break;
                case "potion":
                    {
                        Point fiole = o.M1 + new Vector(3, -5);
                        bool vue = Icone(dc, Textures.Teintee("item/potion_overlay", Color.FromRgb(0xF8, 0x24, 0xA0)), fiole, 24, 0, e, s);
                        vue |= Icone(dc, Textures.Lire("item/potion"), fiole, 24, 0, e, s);
                        if (!vue) dc.DrawEllipse(Blocs.Pinceau(Color.FromRgb(0xF8, 0x24, 0xA0)), gris, e(fiole), 6 * s, 8 * s);
                        break;
                    }
                case "torche":
                    if (!Icone(dc, Textures.Lire("block/torch"), o.M1 + new Vector(0, -16), 44, 0, e, s))
                    {
                        dc.DrawLine(Plume(Color.FromRgb(0x8B, 0x5A, 0x2B), Math.Max(2, epaisseur * 0.6)), e(o.M1), e(o.M1 + new Vector(0, -20)));
                        dc.DrawEllipse(Blocs.Pinceau(Color.FromRgb(0xFF, 0xD0, 0x40)), null, e(o.M1 + new Vector(0, -23)), 4 * s, 4 * s);
                    }
                    break;
                case "wagon":
                    if (!Icone(dc, Textures.Lire("item/minecart"), new Point(o.Hanche.X, -24), 64, 0, e, s))
                        dc.DrawRectangle(Blocs.Pinceau(Color.FromRgb(0x8E, 0x8E, 0x8E)), gris, new Rect(e(new Point(o.Hanche.X - 26, -26)), e(new Point(o.Hanche.X + 26, -2))));
                    break;
                case "bateau":
                    dc.DrawLine(Plume(Color.FromArgb(170, 0x3F, 0x76, 0xE4), 4 * s), e(new Point(o.Hanche.X - 62, -2)), e(new Point(o.Hanche.X + 62, -2)));      // un filet d'eau
                    if (!Icone(dc, Textures.Lire("item/oak_boat"), new Point(o.Hanche.X, -22), 64, 0, e, s))
                        dc.DrawRectangle(Blocs.Pinceau(Color.FromRgb(0xA9, 0x86, 0x4F)), gris, new Rect(e(new Point(o.Hanche.X - 30, -20)), e(new Point(o.Hanche.X + 30, -4))));
                    break;
                case "slime":
                    Bloc(dc, Blocs.Slime, -Biblio.Bloc / 2, 0, e, couleur);
                    break;
                case "craft":
                    Bloc(dc, Blocs.Etabli, 34, 0, e, couleur);
                    if (phase > 0.6)                             // ce qu'il vient de fabriquer s'élève au-dessus de l'établi
                        Icone(dc, Textures.Lire("item/diamond_sword"), new Point(34 + Biblio.Bloc / 2, -Biblio.Bloc - 18 - 40 * (phase - 0.6)), 30, 0, e, s);
                    break;
                case "lit":
                    {
                        // un lit tout simple : matelas, oreiller du côté de la tête, couverture rouge sur les jambes, pieds en bois
                        Brush boisLit = Blocs.Pinceau(Color.FromRgb(0x8B, 0x5A, 0x2B));
                        dc.DrawRectangle(Blocs.Pinceau(Color.FromRgb(0xDD, 0xDD, 0xDD)), null, new Rect(e(new Point(-54, -16)), e(new Point(54, -9))));
                        dc.DrawRectangle(Blocs.Pinceau(Color.FromRgb(0xF6, 0xF6, 0xF6)), null, new Rect(e(new Point(-54, -24)), e(new Point(-30, -16))));
                        dc.DrawRectangle(Blocs.Pinceau(Color.FromRgb(0xB0, 0x2E, 0x26)), null, new Rect(e(new Point(-4, -32)), e(new Point(54, -12))));
                        dc.DrawRectangle(boisLit, null, new Rect(e(new Point(-54, -9)), e(new Point(-47, 0))));
                        dc.DrawRectangle(boisLit, null, new Rect(e(new Point(47, -9)), e(new Point(54, 0))));
                        dc.DrawText(Texte("z Z z".Substring(0, 1 + 2 * (int)(phase * 2.99)), 13 * s, clair, true), new Point(tete.X - 4 * s, tete.Y - 34 * s));
                        break;
                    }
                case "arbre":
                    Bloc(dc, Blocs.Tronc, 36, 0, e, couleur, 1);
                    Bloc(dc, Blocs.Tronc, 36, Biblio.Bloc, e, couleur, 2);
                    Bloc(dc, Blocs.Feuilles, 36, 2 * Biblio.Bloc, e, couleur, 3);
                    if (phase > 0.42 && phase < 0.62)
                        for (int i = 0; i < 3; i++)
                            dc.DrawRectangle(Blocs.Pinceau(Color.FromRgb(0xA9, 0x86, 0x4F)), null, new Rect(e(new Point(30 - i * 6, -52 - 40 * (phase - 0.42) * (1 + i))), new Size(4 * s, 4 * s)));
                    break;
                case "pioche":
                    {
                        Point bout = o.M1 + avantBras * 26;
                        Vector travers = new Vector(-avantBras.Y, avantBras.X);
                        Bloc(dc, Blocs.Diamant, 44, 0, e, couleur, 3);      // le bloc qu'il mine, posé devant lui
                        if (!Outil(dc, "item/diamond_pickaxe", o, avantBras, 44, e, s))
                        {
                            dc.DrawLine(Plume(Color.FromRgb(0x8B, 0x5A, 0x2B), Math.Max(2, epaisseur * 0.6)), e(o.M1 - avantBras * 4), e(bout));
                            Pen diamant = Plume(Color.FromRgb(0x4A, 0xED, 0xD9), Math.Max(2.5, epaisseur * 0.75));
                            dc.DrawLine(diamant, e(bout - travers * 13 - avantBras * 5), e(bout));
                            dc.DrawLine(diamant, e(bout), e(bout + travers * 13 - avantBras * 5));
                        }
                        if (phase > 0.42 && phase < 0.62)                      // éclats au moment du coup
                            for (int i = 0; i < 4; i++)
                                dc.DrawRectangle(clair, null, new Rect(e(new Point(46 + i * 7, -Biblio.Bloc - 6 - 30 * (phase - 0.42) * (1 + i % 2))), new Size(3 * s, 3 * s)));
                        break;
                    }
                case "marteau":
                    {
                        Point bout = o.M1 + avantBras * 40;
                        Vector travers = new Vector(-avantBras.Y, avantBras.X);
                        dc.DrawLine(gris, e(o.M1), e(bout));
                        dc.DrawLine(Plume(Color.FromRgb(0xC8, 0xC8, 0xD0), epaisseur * 2.4), e(bout - travers * 11), e(bout + travers * 11));
                        if (phase > 0.36 && phase < 0.8)
                        {
                            double k = (phase - 0.36) / 0.44;
                            dc.DrawEllipse(null, Plume(Color.FromArgb((byte)(220 * (1 - k)), 255, 255, 255), epaisseur * 0.6), e(new Point(o.M1.X + 20, -2)), (16 + 120 * k) * s, (3 + 12 * k) * s);
                        }
                        break;
                    }
                case "javelot":
                    {
                        double k = Math.Max(0, phase - 0.39);
                        Point centre = phase < 0.39 ? o.M1 : new Point(o.M1.X + 760 * k, o.M1.Y - 20 - 120 * k + 500 * k * k);
                        Vector axe = phase < 0.39 ? new Vector(0.95, -0.3) : new Vector(1, -0.25 + 1.2 * k);
                        axe.Normalize();
                        dc.DrawLine(gris, e(centre - axe * 34), e(centre + axe * 34));
                        break;
                    }
                case "barre":
                    {
                        double y = Math.Min(o.M1.Y, o.M2.Y) - 2;
                        dc.DrawLine(Plume(Color.FromRgb(0xC8, 0xC8, 0xD0), Math.Max(3, epaisseur * 0.8)), e(new Point(o.M1.X - 34, y)), e(new Point(o.M1.X + 34, y)));
                        break;
                    }
                case "cerceau":
                    dc.DrawEllipse(null, Plume(Color.FromRgb(0xFF, 0x4D, 0x9A), Math.Max(2, epaisseur * 0.5)), e(new Point(o.Hanche.X + 9 * Math.Cos(f), o.Hanche.Y - 6)), 27 * s, 7 * s);
                    break;
                case "balai":
                    {
                        Vector axe = o.M1 - o.M2;
                        if (axe.Length < 3) axe = new Vector(0.5, 1);
                        axe.Normalize();
                        Point bas = o.M1 + axe * Math.Max(10, Math.Min(70, -o.M1.Y / Math.Max(0.2, axe.Y)));      // prolongé jusqu'au sol
                        dc.DrawLine(gris, e(o.M2 - axe * 10), e(bas));
                        dc.DrawLine(Plume(Color.FromRgb(0xE0, 0xB0, 0x60), epaisseur * 1.2), e(bas + new Vector(-8, -3)), e(bas + new Vector(8, -3)));
                        break;
                    }
                case "canne":
                    {
                        Point bout = o.M1 + new Vector(46, -34), flotteur = new Point(bout.X + 4 * Math.Sin(f), bout.Y + 96);
                        dc.DrawLine(fin, e(o.M1), e(bout));
                        dc.DrawLine(Plume(Color.FromArgb(180, 255, 255, 255), 1), e(bout), e(flotteur));
                        dc.DrawEllipse(Brushes.OrangeRed, null, e(flotteur), 3 * s, 3 * s);
                        break;
                    }
                case "parapluie":
                    {
                        Point sommet = o.M1 + new Vector(0, -46);
                        dc.DrawLine(fin, e(o.M1), e(sommet));
                        var dome = new StreamGeometry();
                        using (StreamGeometryContext g = dome.Open()) { g.BeginFigure(e(sommet + new Vector(-30, 8)), true, true); g.QuadraticBezierTo(e(sommet + new Vector(0, -26)), e(sommet + new Vector(30, 8)), true, true); }
                        dc.DrawGeometry(teinte, gris, dome);
                        break;
                    }
                case "guitare":
                    {
                        Point corps = o.Hanche + (o.Cou - o.Hanche) * 0.42 + new Vector(9, 0);      // contre le ventre, le manche vers l'autre main
                        Vector manche = o.M2 - corps;
                        if (manche.Length > 0.01) manche.Normalize();
                        dc.DrawLine(gris, e(corps), e(o.M2 + manche * 7));
                        dc.DrawEllipse(teinte, fin, e(corps), 10 * s, 8 * s);
                        break;
                    }
                case "micro":
                    dc.DrawLine(gris, e(o.M1), e(o.M1 + new Vector(3, -9)));
                    dc.DrawEllipse(clair, null, e(o.M1 + new Vector(4, -12)), 4 * s, 4 * s);
                    break;
                case "ballonjongle":
                    dc.DrawEllipse(clair, fin, e(new Point(o.P1.X + 6, o.P1.Y - 9 - 55 * Math.Abs(Math.Cos(f / 2)))), 7 * s, 7 * s);
                    break;
                case "rayon":
                    if (phase > 0.45 && phase < 0.86)
                    {
                        Point mains = new Point((o.M1.X + o.M2.X) / 2 + 6, (o.M1.Y + o.M2.Y) / 2);
                        double vibre = 1 + 0.25 * Math.Sin(phase * 90);
                        dc.DrawLine(Plume(Color.FromArgb(150, couleur.R, couleur.G, couleur.B), epaisseur * 2.6 * vibre), e(mains), e(mains + new Vector(150, 0)));
                        dc.DrawLine(Plume(Colors.White, epaisseur * 0.9 * vibre), e(mains), e(mains + new Vector(150, 0)));
                    }
                    break;
                case "arc":
                    {
                        Point poing = o.M2;
                        var courbe = new StreamGeometry();
                        using (StreamGeometryContext g = courbe.Open()) { g.BeginFigure(e(poing + new Vector(-4, -26)), false, false); g.QuadraticBezierTo(e(poing + new Vector(14, 0)), e(poing + new Vector(-4, 26)), true, true); }
                        dc.DrawGeometry(null, gris, courbe);
                        double vol = Math.Max(0, phase - 0.52);
                        Point fleche = phase < 0.52 ? new Point(Math.Min(poing.X - 8, o.M1.X), poing.Y) : new Point(poing.X + 700 * vol, poing.Y);
                        if (phase > 0.15) dc.DrawLine(fin, e(fleche), e(fleche + new Vector(30, 0)));
                        if (phase < 0.52) { dc.DrawLine(fin, e(poing + new Vector(-4, -26)), e(o.M1)); dc.DrawLine(fin, e(poing + new Vector(-4, 26)), e(o.M1)); }
                        break;
                    }
                case "shuriken":
                    if (phase > 0.32)
                    {
                        double k = phase - 0.32;
                        Point centre = new Point(o.M1.X + 520 * k, o.M1.Y - 10);
                        Vector branche = new Vector(Math.Cos(40 * k), Math.Sin(40 * k)) * 7, autre = new Vector(-branche.Y, branche.X);
                        dc.DrawLine(gris, e(centre - branche), e(centre + branche));
                        dc.DrawLine(gris, e(centre - autre), e(centre + autre));
                    }
                    break;
                case "bouclier":
                    {
                        var courbe = new StreamGeometry();
                        Point m = o.M1 + new Vector(6, 0);
                        using (StreamGeometryContext g = courbe.Open()) { g.BeginFigure(e(m + new Vector(-6, -24)), false, false); g.QuadraticBezierTo(e(m + new Vector(14, 0)), e(m + new Vector(-6, 24)), true, true); }
                        dc.DrawGeometry(null, Plume(Color.FromRgb(0xE8, 0xE8, 0xEE), Math.Max(3, epaisseur)), courbe);
                        break;
                    }
                case "jongle":
                    for (int i = 0; i < 3; i++)
                    {
                        double k = (phase + i / 3.0) % 1;
                        Point balle = new Point(o.M2.X + (o.M1.X - o.M2.X) * k + 4, Math.Min(o.M1.Y, o.M2.Y) - 6 - 150 * k * (1 - k));
                        dc.DrawEllipse(clair, null, e(balle), 4.5 * s, 4.5 * s);
                    }
                    break;
                case "yoyo":
                    {
                        Point bas = new Point(o.M1.X, o.M1.Y + 8 + 34 * (0.5 - 0.5 * Math.Cos(f)));
                        dc.DrawLine(fin, e(o.M1), e(bas));
                        dc.DrawEllipse(teinte, fin, e(bas), 5 * s, 5 * s);
                        break;
                    }
                case "boule":
                    {
                        Point mains = new Point((o.M1.X + o.M2.X) / 2, (o.M1.Y + o.M2.Y) / 2);
                        double charge = Math.Min(1, phase / 0.55), tir = Math.Max(0, phase - 0.57);
                        Point centre = e(new Point(mains.X + 8 + 700 * tir, mains.Y));
                        double rayon = (4 + 12 * charge) * s;
                        var halo = new RadialGradientBrush(Color.FromArgb(230, 255, 255, 255), Color.FromArgb(0, couleur.R, couleur.G, couleur.B));
                        dc.DrawEllipse(halo, null, centre, rayon * 2.2, rayon * 2.2);
                        dc.DrawEllipse(teinte, null, centre, rayon * 0.7, rayon * 0.7);
                        break;
                    }
                case "onde":
                    if (phase > 0.43)
                    {
                        double k = (phase - 0.43) / 0.57;
                        var plume = Plume(Color.FromArgb((byte)(220 * (1 - k)), couleur.R, couleur.G, couleur.B), epaisseur * 0.7);
                        dc.DrawEllipse(null, plume, e(new Point(o.Hanche.X, -2)), (20 + 150 * k) * s, (4 + 16 * k) * s);
                    }
                    break;
                case "aura":
                    for (int i = 0; i < 7; i++)
                    {
                        double k = (phase * 3 + i * 0.37) % 1, x = o.Hanche.X - 30 + i * 10 + 4 * Math.Sin(i * 2.1);
                        var plume = Plume(Color.FromArgb((byte)(200 * (1 - k)), couleur.R, couleur.G, couleur.B), epaisseur * 0.45);
                        dc.DrawLine(plume, e(new Point(x, -10 - 90 * k)), e(new Point(x, -24 - 90 * k)));
                    }
                    break;
                case "zzz": case "coeur": case "note": case "exclam": case "question":
                    {
                        string signe = objet == "zzz" ? "z" : objet == "coeur" ? "♥" : objet == "note" ? "♪" : objet == "exclam" ? "!" : "?";
                        Brush pinceau = objet == "coeur" ? new SolidColorBrush(Color.FromRgb(0xFF, 0x4D, 0x6D)) : clair;
                        bool monte = objet != "exclam" && objet != "question";
                        for (int i = 0; i < (monte ? 3 : 1); i++)
                        {
                            double k = monte ? (phase + i / 3.0) % 1 : 0.2;
                            var texte = Texte(signe, (objet == "zzz" ? 10 + 8 * k : 18) * s, pinceau, true);
                            texte.SetForegroundBrush(new SolidColorBrush(Color.FromArgb((byte)(255 * (monte ? 1 - k : 1)), ((SolidColorBrush)pinceau).Color.R, ((SolidColorBrush)pinceau).Color.G, ((SolidColorBrush)pinceau).Color.B)));
                            dc.DrawText(texte, new Point(tete.X + (14 + 14 * k) * s, tete.Y - (22 + 34 * k) * s));
                        }
                        break;
                    }
            }
        }
    }

    // ============================================================ sons
    // Aucun fichier audio : chaque bruitage est une petite onde calculée au démarrage (glissés de
    // fréquence, souffle), gardée en mémoire et jouée par Windows.
    static class Sons
    {
        const int Hz = 22050;
        static readonly Dictionary<string, IntPtr> ondes = new Dictionary<string, IntPtr>();
        static double volumeConstruit = -1;
        static uint graine = 12345;

        [DllImport("winmm.dll")]
        static extern bool PlaySound(IntPtr son, IntPtr module, uint drapeaux);

        public static void Jouer(string nom, string groupe)
        {
            if (nom == null || !R.O("sons") || !R.O(groupe)) return;
            double volume = R.D("volume") / 100;
            if (volume <= 0.005) return;
            if (Math.Abs(volume - volumeConstruit) > 0.001) Construire(volume);
            IntPtr onde;
            if (ondes.TryGetValue(nom, out onde)) PlaySound(onde, IntPtr.Zero, 0x0001 | 0x0004 | 0x0002);    // asynchrone, en mémoire, sans son par défaut
        }

        static double Souffle() { graine = graine * 1664525 + 1013904223; return (graine >> 8) / 8388608.0 - 1; }

        static void Construire(double volume)
        {
            PlaySound(IntPtr.Zero, IntPtr.Zero, 0);                                   // rien ne doit jouer pendant qu'on remplace les ondes
            foreach (IntPtr ancienne in ondes.Values) Marshal.FreeHGlobal(ancienne);
            ondes.Clear();
            volumeConstruit = volume;
            // (durée, fréquence au fil du temps, part de souffle, vitesse d'extinction)
            Onde("saut", 0.17, t => 260 + 3000 * t, 0, 9, volume);
            Onde("grandsaut", 0.30, t => 200 + 2400 * t, 0, 5, volume);
            Onde("atterrit", 0.14, t => 150 - 650 * t, 0.35, 22, volume);
            Onde("rebond", 0.15, t => 190 + 2600 * t * (1 - t / 0.15), 0.1, 12, volume);
            Onde("coup", 0.09, t => 110 - 500 * t, 0.7, 30, volume);
            Onde("swish", 0.16, t => 0, 1, 14, volume * 0.6, true);
            Onde("lance", 0.28, t => 0, 1, 7, volume * 0.5, true);
            Onde("attrape", 0.12, t => t < 0.06 ? 520 : 820, 0, 14, volume * 0.8);
            Onde("aie", 0.22, t => 720 - 2100 * t, 0.05, 7, volume);
            Onde("glisse", 0.32, t => 950 - 2000 * t, 0, 5, volume * 0.8);
            Onde("pop", 0.09, t => 800 + 7000 * t, 0, 20, volume * 0.8);
            Onde("note", 0.30, t => t < 0.13 ? 660 : 880, 0, 7, volume * 0.7);
            Onde("tada", 0.42, t => t < 0.1 ? 523 : t < 0.2 ? 659 : t < 0.3 ? 784 : 1047, 0, 4, volume * 0.7);
            Onde("energie", 0.75, t => 110 + 700 * t * t + 25 * Math.Sin(60 * t), 0.15, 1.2, volume * 0.8);
        }

        static void Onde(string nom, double duree, Func<double, double> frequence, double souffle, double extinction, double volume, bool sifflant = false)
        {
            int n = (int)(duree * Hz);
            IntPtr memoire = Marshal.AllocHGlobal(44 + 2 * n);
            var octets = new byte[44 + 2 * n];
            Array.Copy(Encoding.ASCII.GetBytes("RIFF"), 0, octets, 0, 4);
            BitConverter.GetBytes(36 + 2 * n).CopyTo(octets, 4);
            Array.Copy(Encoding.ASCII.GetBytes("WAVEfmt "), 0, octets, 8, 8);
            BitConverter.GetBytes(16).CopyTo(octets, 16);
            BitConverter.GetBytes((short)1).CopyTo(octets, 20);
            BitConverter.GetBytes((short)1).CopyTo(octets, 22);
            BitConverter.GetBytes(Hz).CopyTo(octets, 24);
            BitConverter.GetBytes(Hz * 2).CopyTo(octets, 28);
            BitConverter.GetBytes((short)2).CopyTo(octets, 32);
            BitConverter.GetBytes((short)16).CopyTo(octets, 34);
            Array.Copy(Encoding.ASCII.GetBytes("data"), 0, octets, 36, 4);
            BitConverter.GetBytes(2 * n).CopyTo(octets, 40);
            double angle = 0, filtre = 0;
            for (int i = 0; i < n; i++)
            {
                double t = i / (double)Hz;
                angle += 2 * Math.PI * Math.Max(0, frequence(t)) / Hz;
                double bruit = Souffle();
                // un « swish » : du souffle dont on ne garde que l'aigu, qui enfle puis retombe
                filtre += (bruit - filtre) * (sifflant ? 0.5 : 0.18);
                double enveloppe = sifflant ? Math.Sin(Math.PI * t / duree) : Math.Min(1, t / 0.004) * Math.Exp(-extinction * t);
                double ton = Math.Sin(angle) + 0.3 * Math.Sin(2 * angle);
                double valeur = ((1 - souffle) * ton * 0.75 + souffle * (sifflant ? bruit - filtre : filtre) * 1.4) * enveloppe * volume;
                if (i > n - 200) valeur *= (n - i) / 200.0;                              // pas de claquement à la fin
                BitConverter.GetBytes((short)(Math.Max(-1, Math.Min(1, valeur)) * 30000)).CopyTo(octets, 44 + 2 * i);
            }
            Marshal.Copy(octets, 0, memoire, octets.Length);
            ondes[nom] = memoire;
            if (export != null) File.WriteAllBytes(Path.Combine(export, nom + ".wav"), octets);
        }

        // MascotteStickman.exe --sons dossier : écrit les bruitages en .wav, pour les écouter ou les contrôler.
        static string export;
        public static void Exporter(string dossier)
        {
            Directory.CreateDirectory(dossier);
            export = dossier;
            Construire(R.D("volume") / 100);
            export = null;
        }
    }

    sealed class Toile : FrameworkElement
    {
        public Action<DrawingContext> Peindre;
        protected override void OnRender(DrawingContext dc) { if (Peindre != null) Peindre(dc); }
    }

    // ============================================================ textures de Minecraft
    // Les vraies textures, au pixel près : elles sont lues dans le jeu installé sur ce PC (le .jar de la
    // version la plus récente), jamais copiées dans le programme. Sans Minecraft, ou si le réglage est
    // décoché, les blocs sont dessinés par le programme et les objets remplacés par de simples formes.
    static class Textures
    {
        static readonly Dictionary<string, BitmapSource> cache = new Dictionary<string, BitmapSource>();
        static string jar;
        static bool cherche;

        static void Chercher()
        {
            if (cherche) return;
            cherche = true;
            try
            {
                var dossiers = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @".minecraft\versions"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"curseforge\minecraft\Install\versions"),
                };
                foreach (FileInfo f in dossiers.Where(Directory.Exists)
                    .SelectMany(d => new DirectoryInfo(d).GetFiles("*.jar", SearchOption.AllDirectories))
                    .Where(f => f.Length > 8000000).OrderByDescending(f => f.LastWriteTimeUtc))
                    using (ZipArchive z = ZipFile.OpenRead(f.FullName))
                        if (z.GetEntry("assets/minecraft/textures/block/dirt.png") != null) { jar = f.FullName; return; }
            }
            catch (Exception) { }                                // dossier illisible, jar abîmé… : tant pis, blocs dessinés
        }

        // nom : "block/dirt", "item/diamond_pickaxe"… ; null si la texture n'est pas disponible
        public static BitmapSource Lire(string nom)
        {
            if (!R.O("texturesMinecraft")) return null;
            BitmapSource image;
            if (cache.TryGetValue(nom, out image)) return image;
            Chercher();
            if (jar != null)
                try
                {
                    using (ZipArchive z = ZipFile.OpenRead(jar))
                    {
                        ZipArchiveEntry entree = z.GetEntry("assets/minecraft/textures/" + nom + ".png");
                        if (entree != null)
                            using (Stream flux = entree.Open())
                            {
                                var memoire = new MemoryStream();
                                flux.CopyTo(memoire);
                                memoire.Position = 0;
                                BitmapSource brute = new PngBitmapDecoder(memoire, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
                                image = new FormatConvertedBitmap(brute, PixelFormats.Pbgra32, null, 0);
                                if (image.PixelHeight > image.PixelWidth)            // texture animée : sa première image
                                    image = new CroppedBitmap(image, new Int32Rect(0, 0, image.PixelWidth, image.PixelWidth));
                                image.Freeze();
                            }
                    }
                }
                catch (Exception) { image = null; }
            cache[nom] = image;
            return image;
        }

        // La même, multipliée par une couleur : laine, eau et feuilles sont grises dans le jeu, teintées à l'affichage.
        public static BitmapSource Teintee(string nom, Color c)
        {
            BitmapSource grise = Lire(nom), image;
            if (grise == null) return null;
            string cle = nom + "#" + c;
            if (cache.TryGetValue(cle, out image)) return image;
            int l = grise.PixelWidth, h = grise.PixelHeight;
            var pixels = new byte[l * h * 4];
            grise.CopyPixels(pixels, l * 4, 0);
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = (byte)(pixels[i] * c.B / 255);
                pixels[i + 1] = (byte)(pixels[i + 1] * c.G / 255);
                pixels[i + 2] = (byte)(pixels[i + 2] * c.R / 255);
            }
            image = BitmapSource.Create(l, h, 96, 96, PixelFormats.Pbgra32, null, pixels, l * 4);
            image.Freeze();
            cache[cle] = image;
            return image;
        }
    }

    // ============================================================ blocs
    static class Blocs
    {
        public const int Herbe = 0, Pierre = 1, Planches = 2, Laine = 3, Diamant = 4, Terre = 5, Roche = 6, Tronc = 7, Briques = 8, Or = 9,
            Obsidienne = 10, Foin = 11, Pasteque = 12, Bibliotheque = 13, Tnt = 14, Slime = 15, Etabli = 16, Eau = 17, Feuilles = 18, Portail = 19;
        // pour chaque bloc : sa texture dans le jeu, et le dessin de secours qui lui ressemble le plus
        static readonly string[] noms = { "grass_block_side", "cobblestone", "oak_planks", "white_wool", "diamond_ore", "dirt", "stone", "oak_log", "bricks", "gold_block",
            "obsidian", "hay_block_side", "melon_side", "bookshelf", "tnt_side", "slime_block", "crafting_table_front", "water_still", "oak_leaves", "nether_portal" };
        static readonly int[] secours = { 0, 1, 2, 3, 4, 5, 1, 2, 6, 7, 8, 7, 9, 2, 10, 9, 2, 11, 9, 12 };
        public static readonly int[] PourBatir = { Herbe, Pierre, Planches, Laine, Diamant, Terre, Roche, Tronc, Briques, Or, Obsidienne, Foin, Pasteque, Bibliotheque };

        static readonly Dictionary<Color, Brush> pinceaux = new Dictionary<Color, Brush>();
        static readonly Color[] terre = { Rvb(0x866043), Rvb(0x79553A), Rvb(0x96704F), Rvb(0x6B4A32) };
        static readonly Color[] herbe = { Rvb(0x5DA13A), Rvb(0x6CBA45), Rvb(0x4E8F30) };
        static readonly Color[] pierre = { Rvb(0x7F7F7F), Rvb(0x8E8E8E), Rvb(0x6B6B6B), Rvb(0x9A9A9A), Rvb(0x5E5E5E) };
        static readonly Color[] bois = { Rvb(0xB8945F), Rvb(0xA9864F), Rvb(0xC4A06A) };

        static Color Rvb(int v) { return Color.FromRgb((byte)(v >> 16), (byte)(v >> 8), (byte)v); }

        public static Brush Pinceau(Color c)
        {
            Brush b;
            if (!pinceaux.TryGetValue(c, out b)) { b = new SolidColorBrush(c); b.Freeze(); pinceaux[c] = b; }
            return b;
        }

        static Color Nuance(Color c, int h)
        {
            int d = h % 3 * 10 - 10;
            return Color.FromArgb(c.A, (byte)Math.Max(0, Math.Min(255, c.R + d)), (byte)Math.Max(0, Math.Min(255, c.G + d)), (byte)Math.Max(0, Math.Min(255, c.B + d)));
        }

        static Color Teinte(int type, int i, int j, int h, Color laine)
        {
            switch (type)
            {
                case 0: return j < 2 || (j == 2 && h % 3 == 0) ? herbe[h % herbe.Length] : terre[h % terre.Length];
                case 2: return j % 4 == 3 ? Rvb(0x7E6237) : bois[h % bois.Length];
                case 3: return Nuance(laine, h);
                case 4: return h % 9 == 0 || h % 9 == 4 ? Rvb(0x4AEDD9) : pierre[h % pierre.Length];
                case 5: return terre[h % terre.Length];
                case 6: return j % 4 == 3 || (i + (j / 4 % 2) * 4) % 8 == 7 ? Rvb(0xB8B0A8) : Nuance(Rvb(0x9A4A38), h);
                case 7: return Nuance(Rvb(0xF2D440), h);
                case 8: return Nuance(Rvb(0x1E1830), h);
                case 9: return Nuance(Rvb(0x62B84A), h);
                case 10: return j >= 3 && j <= 4 ? Rvb(0xF0F0F0) : Nuance(Rvb(0xD83020), h);
                case 11: return Nuance(Color.FromArgb(190, 0x3F, 0x76, 0xE4), h);
                case 12: return Nuance(Color.FromArgb(200, 0x8A, 0x30, 0xD8), h);
                default: return pierre[h % pierre.Length];
            }
        }

        public static void Dessiner(DrawingContext dc, Rect r, int bloc, Color laine, int graine)
        {
            string nom = "block/" + noms[bloc];
            BitmapSource image = bloc == Laine ? Textures.Teintee(nom, laine)
                : bloc == Eau ? Textures.Teintee(nom, Color.FromRgb(0x3F, 0x76, 0xE4))
                : bloc == Feuilles ? Textures.Teintee(nom, Color.FromRgb(0x59, 0xAE, 0x30))
                : Textures.Lire(nom);
            if (image != null) { dc.DrawImage(image, r); return; }

            const int n = 8;                                     // dessin de secours : huit gros pixels de côté
            double c = r.Width / n;
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    int h = ((i * 73856093) ^ (j * 19349663) ^ ((graine + 1 + bloc * 7) * 83492791)) & 0x7FFFFFFF;
                    dc.DrawRectangle(Pinceau(Teinte(secours[bloc], i, j, h % 1009, laine)), null, new Rect(r.X + i * c - 0.25, r.Y + j * c - 0.25, c + 0.5, c + 0.5));
                }
            dc.DrawRectangle(null, new Pen(Pinceau(Color.FromArgb(110, 0, 0, 0)), 1), r);
        }
    }

    // Un décor ou un objet qui vole (tour de blocs, TNT, portail, perle, fusée…) : une fenêtre à part,
    // que les clics traversent, et que la scène en cours dessine comme elle veut.
    sealed class Decor : Window
    {
        readonly Toile toile = new Toile();
        public Action<DrawingContext, double, double> Peindre;       // (dc, largeur, hauteur)

        public Decor()
        {
            Title = "Décor du stickman";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            ShowActivated = false;
            Focusable = false;
            IsHitTestVisible = false;
            WindowStartupLocation = WindowStartupLocation.Manual;
            RenderOptions.SetBitmapScalingMode(toile, BitmapScalingMode.NearestNeighbor);      // les textures gardent leurs pixels nets
            Content = toile;
            toile.Peindre = dc => { if (Peindre != null) Peindre(dc, Width, Height); };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr h = new WindowInteropHelper(this).Handle;
            SetWindowLong(h, -20, GetWindowLong(h, -20) | 0x80 | 0x20 | 0x08000000);      // outil, transparente aux clics, jamais active
        }

        public void Poser(double gauche, double haut, double largeur, double hauteur, bool devant)
        {
            Width = Math.Max(1, largeur); Height = Math.Max(1, hauteur);
            Left = gauche; Top = haut;
            Topmost = devant;
            if (!IsVisible) Show();
            toile.InvalidateVisual();
        }

        public void Redessiner() { toile.InvalidateVisual(); }

        public void Cacher() { if (IsVisible) Hide(); }

        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr fenetre, int index);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr fenetre, int index, int valeur);
    }

    // Planches de contrôle : chaque animation en huit images, pour vérifier les poses d'un coup d'œil.
    static class Planches
    {
        // --web dossier : animations.json pour la version web. Chaque animation y devient une suite de poses
        // (13 entiers, dans l'ordre de I) prises à intervalles réguliers ; la page les relie entre elles.
        // Restent ici : les scènes entières, les animations de rebord de fenêtre et la famille Minecraft.
        public static void ExporterWeb(string dossier)
        {
            Directory.CreateDirectory(dossier);
            CultureInfo inv = CultureInfo.InvariantCulture;
            Func<string, string> texte = t => "\"" + (t ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            Func<double[], string> pose = p => "[" + string.Join(",", p.Select(v => Math.Round(v).ToString(inv))) + "]";
            Func<Anim, string> anim = a =>
            {
                int n = Math.Max(12, Math.Min(90, (int)Math.Round(a.Duree * 30)));
                var poses = new List<string>();
                for (int i = 0; i <= n; i++) poses.Add(pose(a.Pose(i / (double)n)));
                return "{\"n\":" + texte(a.Nom) + ",\"f\":" + texte(a.Famille) + ",\"d\":" + a.Duree.ToString("0.###", inv) + ",\"t\":" + a.Tours
                    + (a.Haut ? ",\"h\":1" : "") + (a.Deplace ? ",\"m\":1,\"v\":" + a.Vitesse.ToString("0.#", inv) : "")
                    + (a.Objet != null ? ",\"o\":" + texte(a.Objet) : "") + ",\"p\":[" + string.Join(",", poses) + "]}";
            };
            List<Anim> liste = Biblio.Toutes.Where(a => a.Special == null && !a.SurFenetre && a.Famille != "Minecraft").ToList();
            string json = "{\"repos\":[" + string.Join(",", Biblio.Repos.Select(anim)) + "],\n\"reception\":" + anim(Biblio.Reception) + ",\n\"releve\":" + anim(Biblio.SeReleve)
                + ",\n\"monte\":" + pose(Biblio.SautMonte) + ",\"descend\":" + pose(Biblio.SautDescend) + ",\"marche\":" + texte(Biblio.Marche.Nom) + ",\"course\":" + texte(Biblio.Course.Nom)
                + ",\n\"anims\":[\n" + string.Join(",\n", liste.Select(anim)) + "\n]}\n";
            File.WriteAllText(Path.Combine(dossier, "animations.json"), json, new UTF8Encoding(false));
        }

        // --planches dossier duos : les animations à deux, les deux stickmen face à face à leur distance
        static void EcrireDuos(string dossier)
        {
            List<Duo> liste = Biblio.Duos.Where(d => d.A != null).ToList();
            const int images = 8;
            const double cl = 150, ch = 108, marge = 190, echelle = 0.55;
            Pen un = Dessin.Plume(Color.FromRgb(0xFF, 0x8A, 0x1A), 3.5), deux = Dessin.Plume(Color.FromRgb(0x1E, 0x88, 0xE5), 3.5);
            var visuel = new DrawingVisual();
            using (DrawingContext dc = visuel.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x24, 0x26, 0x2B)), null, new Rect(0, 0, marge + images * cl, liste.Count * ch));
                for (int i = 0; i < liste.Count; i++)
                {
                    Duo d = liste[i];
                    dc.DrawText(Dessin.Texte(d.Nom, 12, Brushes.White), new Point(6, i * ch + 48));
                    dc.DrawLine(new Pen(Brushes.DimGray, 1), new Point(marge, (i + 1) * ch - 6), new Point(marge + images * cl, (i + 1) * ch - 6));
                    for (int k = 0; k < images; k++)
                    {
                        double phase = (k + 0.5) / images, oy = (i + 1) * ch - 6, centre = marge + k * cl + cl / 2, demi = d.Distance / 2 * echelle;
                        Dessin.Os a = Dessin.Calculer(d.A.Pose(phase), false), b = Dessin.Calculer(d.B.Pose(phase), false);
                        Dessin.Tracer(dc, a, p => new Point(centre - demi + p.X * echelle, oy + p.Y * echelle), echelle, null, un, null);
                        Dessin.Tracer(dc, b, p => new Point(centre + demi - p.X * echelle, oy + p.Y * echelle), echelle, null, deux, deux.Brush);
                    }
                }
            }
            var image = new RenderTargetBitmap((int)(marge + images * cl), (int)(liste.Count * ch), 96, 96, PixelFormats.Pbgra32);
            image.Render(visuel);
            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(image));
            using (FileStream flux = File.Create(Path.Combine(dossier, "duos.png"))) png.Save(flux);
        }

        public static void Ecrire(string dossier, string filtre)
        {
            Directory.CreateDirectory(dossier);
            if (filtre == "duos") { EcrireDuos(dossier); return; }
            List<Anim> liste = Biblio.Toutes.Where(a => filtre == "" || (a.Famille + " " + a.Nom).IndexOf(filtre, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            const int parPage = 12, images = 8;
            const double cl = 100, ch = 108, marge = 190, echelle = 0.55;
            Pen plume = Dessin.Plume(Color.FromRgb(0xFF, 0x8A, 0x1A), 3.5);
            for (int page = 0; page * parPage < liste.Count; page++)
            {
                var visuel = new DrawingVisual();
                using (DrawingContext dc = visuel.RenderOpen())
                {
                    dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x24, 0x26, 0x2B)), null, new Rect(0, 0, marge + images * cl, parPage * ch));
                    for (int i = 0; i < parPage && page * parPage + i < liste.Count; i++)
                    {
                        Anim a = liste[page * parPage + i];
                        dc.DrawText(Dessin.Texte(a.Famille, 10, Brushes.Gray), new Point(6, i * ch + 34));
                        dc.DrawText(Dessin.Texte(a.Nom, 12, Brushes.White), new Point(6, i * ch + 48));
                        dc.DrawLine(new Pen(Brushes.DimGray, 1), new Point(marge, (i + 1) * ch - 6), new Point(marge + images * cl, (i + 1) * ch - 6));
                        for (int k = 0; k < images; k++)
                        {
                            double phase = k / (double)images;
                            double[] p = a.Pose(phase);
                            if (a.Haut) p = Bonhomme.Superposer(Biblio.Neutre(), p);
                            Dessin.Os o = Dessin.Calculer(p, false);
                            double ox = marge + k * cl + cl / 2, oy = (i + 1) * ch - 6;
                            Func<Point, Point> e = point => new Point(ox + point.X * echelle, oy + point.Y * echelle);
                            Dessin.Tracer(dc, o, e, echelle, null, plume, null);
                            if (a.Objet != null) Dessin.Objet(dc, a.Objet, o, e, echelle, phase, Color.FromRgb(0xFF, 0x8A, 0x1A), 3.5);
                        }
                    }
                }
                var image = new RenderTargetBitmap((int)(marge + images * cl), (int)(parPage * ch), 96, 96, PixelFormats.Pbgra32);
                image.Render(visuel);
                var png = new PngBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(image));
                using (FileStream flux = File.Create(Path.Combine(dossier, "planche-" + (page + 1).ToString("00") + ".png"))) png.Save(flux);
            }
            File.WriteAllText(Path.Combine(dossier, "compte.txt"), Biblio.Toutes.Count + " animations, " + R.Tous.Count(p => p.Cat != "") + " réglages généraux ; doublons : "
                + string.Join(", ", Biblio.Toutes.GroupBy(a => a.Nom).Where(g => g.Count() > 1).Select(g => g.Key)));
        }
    }

    // ============================================================ la mascotte

    sealed class Bonhomme : Window
    {
        enum Etat { Anime, Vol, Porte, Plane }

        // Deux façons de dessiner la tête : un disque plein (la plupart des teintes) ou un simple anneau
        // (l'orange, le noir et le rouge sombre). Un réglage permet de forcer l'une ou l'autre.
        public sealed class Nuance { public string Nom; public int Rvb; public bool Creuse; }

        public static readonly Nuance[] Palette =
        {
            Teinte("Orange", 0xFF8A1A, true), Teinte("Rouge", 0xE53935), Teinte("Vert", 0x43A047), Teinte("Bleu", 0x1E88E5),
            Teinte("Jaune", 0xFDD835), Teinte("Violet", 0x8E24AA), Teinte("Rose", 0xEC407A), Teinte("Cyan", 0x00BCD4),
            Teinte("Noir", 0x111111, true), Teinte("Rouge sombre", 0xB71C1C, true), Teinte("Blanc", 0xFFFFFF), Teinte("Gris", 0x9E9E9E),
            Teinte("Turquoise", 0x26A69A), Teinte("Or", 0xFFC107), Teinte("Citron vert", 0xC6FF00), Teinte("Bleu nuit", 0x3949AB),
        };
        static Nuance Teinte(string nom, int rvb, bool creuse = false) { return new Nuance { Nom = nom, Rvb = rvb, Creuse = creuse }; }

        // Tête creuse ou pleine : réglage forcé, sinon celle de la teinte de la palette la plus proche.
        static bool TeteCreuse(Color couleur)
        {
            int style = (int)R.D("styleTete");
            if (style != 0) return style == 2;
            if (R.O("arcenciel")) return false;
            Nuance proche = null;
            double meilleur = double.MaxValue;
            foreach (Nuance n in Palette)
            {
                Color c = R.Rvb(n.Rvb);
                double ecart = Math.Pow(c.R - couleur.R, 2) + Math.Pow(c.G - couleur.G, 2) + Math.Pow(c.B - couleur.B, 2);
                if (ecart < meilleur) { meilleur = ecart; proche = n; }
            }
            return proche.Creuse;
        }

        readonly Toile toile = new Toile { Cursor = Cursors.Hand };
        readonly Random hasard = new Random();
        static readonly Stopwatch chrono = Stopwatch.StartNew();      // une seule horloge pour tous les stickmen
        readonly DispatcherTimer minuteur = new DispatcherTimer(DispatcherPriority.Render);
        readonly List<Point> trace = new List<Point>();
        Parametres fenetreReglages;

        Etat etat = Etat.Anime;
        Anim courante, geste;
        double tCourante, tGeste, toursCourante, toursGeste, phase;
        Action apres;
        bool enRepos, dort;
        double[] pose = Biblio.Neutre(), fonduDepuis;
        double fondu = 1;

        double s = 1, largeur, hauteur, solY;        // taille et dimensions de la fenêtre
        Point ancre, maison;                          // point au sol sous lui, et sa place habituelle
        int face = 1;
        double cible; bool versCible;
        Vector vitesse; double vrille, rotVol; int rebonds; bool sautVoulu;
        IntPtr poignee, support; double supportX;
        double temps, prochaineAction, finCombat, prochaineCommande, prochaineSauvegarde;

        Point curseur; Vector vCurseur; bool appui, curseurLu; Point appuiCurseur;
        double balance, vBalance;
        string bulle; double finBulle;
        bool sonFait;                                 // le bruitage de l'animation en cours a déjà été joué
        int styleSaut; double tVol, dureeVol;         // saut vers une fenêtre : 0 simple, 1 en salto, 2 atterrissage de héros
        double voile = 1, voileVise = 1;              // fondu de la téléportation
        static double prochaineFermeture;             // mode farceur (un seul rythme pour toute la bande)
        IntPtr victime;
        bool croixPressee;
        Action apresSaut;                            // à faire une fois posé, après un saut voulu
        int bruit;                                    // prochain bruitage de l'animation en cours (celles qui en ont plusieurs)
        bool sautCible; IntPtr cibleSaut;             // saut vers un endroit précis : il ne se pose que là (zéro = le sol)

        // plusieurs stickmen : chacun sa fenêtre, le premier (place 0) commande
        public static readonly List<Bonhomme> Tous = new List<Bonhomme>();
        int place;
        Bonhomme ami; Duo duo;                        // rencontre en cours : avec qui, pour quoi faire
        bool enDuo; double finAttente;

        // musique : l'oreille écoute pour tout le monde
        static Oreille oreille;
        static bool musique; static double periodeMusique = 0.5;
        bool surMusique; double dureeForcee;          // danse calée sur le tempo : durée d'un cycle, en secondes

        string CleCouleur { get { return place == 0 ? "couleur" : "couleur" + (place + 1); } }

        public Bonhomme(int numero)
        {
            place = numero;
            Tous.Add(this);
            temps = chrono.Elapsed.TotalSeconds;
            Title = "Mascotte Stickman";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            ShowActivated = false;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Content = toile;
            RenderOptions.SetBitmapScalingMode(toile, BitmapScalingMode.NearestNeighbor);      // textures de Minecraft : pixels nets
            toile.Peindre = Dessiner;
            toile.MouseLeftButtonDown += (o, e) => { appui = true; appuiCurseur = curseur; toile.CaptureMouse(); };
            toile.MouseLeftButtonUp += (o, e) => Relacher();
            ConstruireMenu();

            Rect zone = SystemParameters.WorkArea;
            maison = new Point(zone.Right - R.D("droite") - 130 * place, zone.Bottom);      // les amis s'alignent à sa gauche
            maison.X = Math.Max(SystemParameters.VirtualScreenLeft + 60, Math.Min(SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 60, maison.X));
            ancre = maison;
            AppliquerUn();
            Repos();
            minuteur.Tick += Tic;
            Loaded += (o, e) =>
            {
                minuteur.Start();
                Jouer(Biblio.Salut, 4);
                Dire("Salut !", 3);
                if (place != 0) return;
                AjusterNombre();
                oreille = new Oreille(
                    (active, duree) => Dispatcher.BeginInvoke(new Action(() => MusiqueChange(active, duree))),
                    fort => Dispatcher.BeginInvoke(new Action(() => DemiTemps(fort)))) { Active = R.O("musique") };
            };
            Closed += (o, e) =>
            {
                minuteur.Stop();                                 // sinon il continuerait à vivre, invisible, et à aller voir les autres
                if (decor != null) decor.Close();
                if (volant != null) volant.Close();
                Rompre();
                Tous.Remove(this);
                R.Enregistrer();
                if (fenetreReglages != null) fenetreReglages.Close();
            };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            poignee = new WindowInteropHelper(this).Handle;
            SetWindowLong(poignee, GWL_EXSTYLE, GetWindowLong(poignee, GWL_EXSTYLE) | WS_EX_TOOLWINDOW);
        }

        // Les réglages qui touchent la fenêtre elle-même ; les autres sont relus à chaque image.
        public void Appliquer()
        {
            foreach (Bonhomme b in Tous) b.AppliquerUn();
            if (oreille != null) oreille.Active = R.O("musique");
        }

        void AppliquerUn()
        {
            s = R.D("taille");
            largeur = 340 * s; hauteur = 360 * s; solY = hauteur - 64 * s;      // sous le sol : la place des jambes qui pendent d'une fenêtre
            Width = largeur; Height = hauteur;
            Topmost = R.O("premierplan");
            minuteur.Interval = TimeSpan.FromMilliseconds(1000 / R.D("fluidite"));
            toile.Effect = R.O("lueur") ? new DropShadowEffect { Color = CouleurDuMoment(), BlurRadius = 16 * s, ShadowDepth = 0, Opacity = 0.95 } : null;
        }

        Color CouleurDuMoment()
        {
            if (!R.O("arcenciel")) return R.Couleur(CleCouleur);
            double h = (temps * R.D("arcVitesse") + place * 67) % 360 / 60, x = 1 - Math.Abs(h % 2 - 1);
            double r = h < 1 ? 1 : h < 2 ? x : h < 4 ? 0 : h < 5 ? x : 1, v = h < 1 ? x : h < 3 ? 1 : h < 4 ? x : 0, b = h < 2 ? 0 : h < 3 ? x : h < 5 ? 1 : x;
            return Color.FromRgb((byte)(255 * r), (byte)(255 * v), (byte)(255 * b));
        }

        // ------------------------------------------------------------ boucle principale

        void Tic(object o, EventArgs e)
        {
            double maintenant = chrono.Elapsed.TotalSeconds, dt = Math.Min(0.05, maintenant - temps);
            temps = maintenant;
            LireCurseur(dt);
            if (appui && etat != Etat.Porte && R.O("attrape") && (curseur - appuiCurseur).Length > 6) Attraper();
            if (place == 0)
            {
                if (temps > prochaineCommande) { prochaineCommande = temps + 0.4; LireCommande(); }
                if (Tous.Count != (int)R.D("nombre") + 1) AjusterNombre();
            }

            switch (etat)
            {
                case Etat.Anime: Animer(dt); break;
                case Etat.Vol: Voler(dt); break;
                case Etat.Porte: Porter(dt); break;
                case Etat.Plane: Planer(dt); break;
            }
            Scene(dt);
            voile += Math.Max(-dt * 5, Math.Min(dt * 5, voileVise - voile));
            toile.Opacity = R.D("opacite") / 100 * voile;
            Placer();
            toile.InvalidateVisual();
            if (R.Sale && temps > prochaineSauvegarde) { prochaineSauvegarde = temps + 2; R.Enregistrer(); }
        }

        void LireCurseur(double dt)
        {
            POINT p;
            GetCursorPos(out p);
            PresentationSource source = PresentationSource.FromVisual(this);
            Point point = (source != null ? source.CompositionTarget.TransformFromDevice : Matrix.Identity).Transform(new Point(p.X, p.Y));
            if (curseurLu && dt > 0) vCurseur = vCurseur * 0.6 + (point - curseur) / dt * 0.4;
            curseur = point;
            curseurLu = true;
        }

        void LireCommande()
        {
            string fichier = Path.Combine(Programme.Dossier, "commande.txt");
            if (!File.Exists(fichier)) return;
            try
            {
                string nom = File.ReadAllText(fichier).Trim();
                File.Delete(fichier);
                // quelques commandes en plus des noms d'animations : @reglages, @fenetre, @couleur RRVVBB
                if (nom == "@reglages") { OuvrirReglages(); return; }
                if (nom == "@fenetre") { if (etat == Etat.Anime) SauterSurFenetre(); return; }
                if (nom.StartsWith("@fermer ")) { if (!FermerUneFenetre(nom.Substring(8).Trim())) Dire("Je ne trouve pas cette fenêtre", 2.5); return; }
                if (nom.StartsWith("@couleur ")) { R.Mettre("couleur", Convert.ToInt32(nom.Substring(9).Trim(), 16)); R.Mettre("arcenciel", 0); Appliquer(); return; }
                // @amis 3 : trois stickmen ; @duo Check : va faire un check à un ami ; @musique : danse sur le tempo du moment
                if (nom.StartsWith("@amis ")) { R.Mettre("nombre", Math.Max(0, Math.Min(5, int.Parse(nom.Substring(6).Trim()) - 1))); return; }
                if (nom.StartsWith("@duo"))
                {
                    string lequel = nom.Substring(4).Trim();
                    Duo d = Biblio.Duos.FirstOrDefault(x => x.Nom.StartsWith(lequel, StringComparison.OrdinalIgnoreCase));
                    if (!Rencontrer(lequel == "" ? null : d)) Dire("Personne n'est libre", 2.5);
                    return;
                }
                if (nom == "@musique") { DanserSurLaMusique(); return; }
                if (nom.StartsWith("@renvoyer ")) { Bonhomme b = Tous.FirstOrDefault(x => x.place == int.Parse(nom.Substring(10).Trim())); if (b != null) b.Renvoyer(); return; }
                Anim a = Biblio.Trouver(nom);
                if (a != null && etat == Etat.Anime && (!a.SurFenetre || support != IntPtr.Zero)) Dire(a.Nom, 2.5);
                if (a != null) JouerDemande(a);
            }
            catch (IOException) { }
        }

        void Placer()
        {
            if (etat == Etat.Porte) { Left = curseur.X - largeur / 2; Top = curseur.Y - hauteur * 0.2; }
            else { Left = ancre.X - largeur / 2; Top = ancre.Y - solY; }
        }

        // ------------------------------------------------------------ animations

        void Fondre()
        {
            fonduDepuis = (double[])pose.Clone();
            fondu = 0;
        }

        public void Jouer(Anim a, double tours, Action ensuite = null)
        {
            if (a == null) return;
            if (a.Haut)                                          // geste du haut du corps : par-dessus ce que font les jambes
            {
                geste = a; tGeste = 0; toursGeste = tours;
                Sons.Jouer(a.Son, "sonsAnimations");
                return;
            }
            Fondre();
            courante = a; tCourante = 0; toursCourante = tours; apres = ensuite;
            versCible = false; enRepos = false; dort = false; sonFait = false;
            bruit = 0; surMusique = false; dureeForcee = 0;
        }

        public void Demander(Anim a) { JouerDemande(a); }

        void JouerDemande(Anim a)
        {
            if (etat != Etat.Anime) return;
            Rompre();
            if (scene == "portail" || scene == "eau" || scene == "perle") RangerScene();
            if (a.Special != null) { Speciale(a); return; }
            if (a.SurFenetre && support == IntPtr.Zero)          // jambes dans le vide : il lui faut un rebord
            {
                Dire(SauterSurFenetre() ? "Il me faut une fenêtre !" : "Aucune fenêtre où m'asseoir", 2.5);
                return;
            }
            if (a.Deplace) AllerVers(Destination(), a, null);
            else Jouer(a, Math.Max(1, Math.Round(a.Tours * R.D("tours") / 100)));
        }

        void Repos()
        {
            Jouer(Biblio.Repos[hasard.Next(Biblio.Repos.Count)], double.MaxValue);
            enRepos = true;
            prochaineAction = temps + R.D("activite") * (0.5 + hasard.NextDouble());
        }

        void Suite()
        {
            Action a = apres;
            apres = null;
            if (a != null) a(); else Repos();
        }

        void AllerVers(double x, Anim marche, Action ensuite)
        {
            double gauche, droite;
            Limites(out gauche, out droite);
            x = Math.Max(gauche, Math.Min(droite, x));
            if (Math.Abs(x - ancre.X) < 8) { if (ensuite != null) ensuite(); else Repos(); return; }
            Jouer(marche, double.MaxValue, ensuite);
            cible = x; versCible = true;
            face = (x > ancre.X ? 1 : -1) * (marche.Vitesse < 0 ? -1 : 1);
        }

        double Destination()
        {
            double centre = R.O("partout") || support != IntPtr.Zero ? ancre.X : maison.X;
            double distance = R.D("distance") * (0.3 + 0.7 * hasard.NextDouble());
            double gauche, droite;
            Limites(out gauche, out droite);
            double x = centre + (hasard.Next(2) == 0 ? -distance : distance);
            if (x < gauche || x > droite) x = centre - (x - centre);
            return Math.Max(gauche, Math.Min(droite, x));
        }

        void Limites(out double gauche, out double droite)
        {
            Bord bord;
            if (support != IntPtr.Zero && LireBord(support, out bord)) { gauche = bord.Gauche + 16; droite = bord.Droite - 16; return; }
            gauche = SystemParameters.VirtualScreenLeft + 30 * s;
            droite = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 30 * s;
        }

        // Applique un geste du haut du corps sur une pose complète.
        public static double[] Superposer(double[] corps, double[] haut)
        {
            var p = (double[])corps.Clone();
            p[I.Torse] += haut[I.Torse];
            p[I.Tete] = haut[I.Tete];
            p[I.Ep1] = haut[I.Ep1]; p[I.Co1] = haut[I.Co1]; p[I.Ep2] = haut[I.Ep2]; p[I.Co2] = haut[I.Co2];
            return p;
        }

        double[] Adoucir(double[] p, double dt)
        {
            if (fondu >= 1) return p;
            double duree = R.D("fondu") / 1000;
            fondu = duree <= 0.001 ? 1 : fondu + dt / duree;
            if (fondu >= 1) return p;
            double k = fondu * fondu * (3 - 2 * fondu);
            double[] q = Biblio.Mix(fonduDepuis, p, k);
            double ecart = Math.IEEERemainder(p[I.Rot] - fonduDepuis[I.Rot], 360);     // la rotation prend le plus court chemin
            q[I.Rot] = fonduDepuis[I.Rot] + ecart * k;
            return q;
        }

        void Animer(double dt)
        {
            if (SupportPerdu()) return;
            if (Bouscule()) return;

            tCourante += dureeForcee > 0 ? dt / dureeForcee : dt * R.D("vitesse") / 100 / courante.Duree;
            if (courante.Deplace && versCible)
            {
                double pas = Math.Abs(courante.Vitesse) * R.D("marche") / 100 * s * dt;
                if (Math.Abs(cible - ancre.X) <= pas) { Glisser(cible - ancre.X); Suite(); return; }
                Glisser(cible > ancre.X ? pas : -pas);
            }
            else
            {
                if (courante.Vitesse != 0) Glisser(face * courante.Vitesse * s * dt, true);
                if (tCourante >= toursCourante) { Suite(); return; }
            }

            phase = tCourante - Math.Floor(tCourante);
            if (!sonFait && courante.Son != null && tCourante >= courante.SonA) { sonFait = true; Sons.Jouer(courante.Son, "sonsAnimations"); }
            if (courante.Bruits != null)
                while (bruit < courante.Bruits.Length && tCourante >= courante.BruitsA[bruit]) Sons.Jouer(courante.Bruits[bruit++], "sonsAnimations");
            double[] p = courante.Pose(phase);
            if (geste != null)
            {
                tGeste += dt * R.D("vitesse") / 100 / geste.Duree;
                if (tGeste >= toursGeste) { geste = null; Fondre(); }
                else p = Superposer(p, geste.Pose(tGeste - Math.Floor(tGeste)));
            }
            if (enRepos && geste == null && R.O("regarde")) p[I.Tete] += Regard();
            p[I.Rot] = Math.IEEERemainder(p[I.Rot], 360);
            pose = Adoucir(p, dt);

            if (enRepos) Vivre();
        }

        void Glisser(double dx, bool borne = false)
        {
            if (borne)
            {
                double gauche, droite;
                Limites(out gauche, out droite);
                dx = Math.Max(gauche, Math.Min(droite, ancre.X + dx)) - ancre.X;
            }
            ancre.X += dx;
            if (support != IntPtr.Zero) supportX += dx;
        }

        double Regard()
        {
            Vector vers = curseur - new Point(ancre.X, ancre.Y - 95 * s);
            if (vers.X * face < 20) return 0;                    // le curseur est derrière : il ne se tord pas le cou
            return Math.Max(-35, Math.Min(30, Math.Atan2(vers.Y, Math.Abs(vers.X)) * 180 / Math.PI * 0.7));
        }

        // Ce qu'il décide de faire quand il n'a rien en cours.
        void Vivre()
        {
            if (ami != null)                                     // un ami arrive : il l'attend sans rien entreprendre
            {
                if (!enDuo) face = ami.ancre.X >= ancre.X ? 1 : -1;
                if (!enDuo && temps > finAttente) Rompre();
                return;
            }
            double dx = curseur.X - ancre.X, haut = ancre.Y - curseur.Y;
            if (!appui)
            {
                if (R.O("combat") && temps > finCombat && Math.Abs(dx) < R.D("portee") * s && Math.Abs(dx) > 14 * s && haut > -20 && haut < 175 * s)
                {
                    face = dx > 0 ? 1 : -1;
                    finCombat = temps + R.D("combatRepos") * (0.6 + 0.8 * hasard.NextDouble());
                    Jouer(haut > 100 * s ? Biblio.PoingHaut : haut > 62 * s ? Biblio.Poing : haut > 30 * s ? Biblio.PiedMoyen : Biblio.PiedBas, 1);
                    return;
                }
                if (R.O("fuit") && Math.Abs(dx) < 170 * s && haut > -40 && haut < 260 * s) { AllerVers(ancre.X - Math.Sign(dx) * 320 * s, Biblio.Course, null); if (!enRepos) return; }
                if (R.O("suit") && Math.Abs(dx) > 150 * s) { AllerVers(curseur.X - Math.Sign(dx) * 80 * s, Math.Abs(dx) > 500 * s ? Biblio.Course : Biblio.Marche, null); if (!enRepos) return; }
            }

            double inactif = SecondesSansSaisie();
            if (R.D("sommeil") > 0 && inactif > R.D("sommeil") * 60 && !dort)
            {
                Jouer(Biblio.Dort, double.MaxValue);
                enRepos = true; dort = true;
                prochaineAction = double.MaxValue;
                return;
            }
            if (dort) { if (inactif < 2) { Jouer(Biblio.SeReleve, 1); Dire("Hein ?!", 2); } return; }
            if (temps >= prochaineAction) Choisir();
        }

        void Choisir()
        {
            // mode farceur : la première fermeture arrive peu après l'activation, les suivantes au rythme réglé
            if (!R.O("ferme")) prochaineFermeture = 0;
            else if (prochaineFermeture == 0) prochaineFermeture = temps + Math.Min(R.D("fermeDelai") * 60, 20 + 25 * hasard.NextDouble());
            else if (temps > prochaineFermeture && FermerUneFenetre(null)) return;
            if (Tous.Count > 1 && R.O("rencontres") && hasard.NextDouble() * 100 < R.D("rencontresChance") && Rencontrer(null)) return;
            if (musique && R.O("musique") && hasard.NextDouble() * 100 < R.D("musiqueChance") && DanserSurLaMusique()) return;
            if (R.O("fenetres") && hasard.NextDouble() * 100 < R.D("fenetresChance") && SauterSurFenetre()) return;
            if (hasard.NextDouble() * 100 < R.D("teleporte") && Teleporter()) return;

            // les animations « jambes dans le vide » ne sont proposées que perché sur une fenêtre
            Func<Anim, bool> permise = a => !R.Coupees.Contains(a.Nom) && (!a.SurFenetre || support != IntPtr.Zero);
            var possibles = new List<Anim>();
            double total = 0;
            var poids = new Dictionary<string, double>();
            foreach (string famille in Biblio.Familles)
            {
                double w = famille == "Déplacements" && !R.O("balade") ? 0 : R.D("poids." + famille);
                if (famille == "Fenêtres" && support != IntPtr.Zero) w *= 3;      // et là, il en profite
                poids[famille] = w;
                if (w > 0 && Biblio.Toutes.Any(a => a.Famille == famille && permise(a))) total += w;
                else poids[famille] = 0;
            }
            if (total <= 0) { prochaineAction = temps + 2; return; }
            double tirage = hasard.NextDouble() * total;
            string choisie = null;
            foreach (string famille in Biblio.Familles)
            {
                if (poids[famille] <= 0) continue;
                choisie = famille;
                tirage -= poids[famille];
                if (tirage <= 0) break;
            }
            foreach (Anim a in Biblio.Toutes) if (a.Famille == choisie && permise(a)) possibles.Add(a);
            Anim suivante = possibles[hasard.Next(possibles.Count)];
            if (suivante.Haut) { Jouer(suivante, suivante.Tours); prochaineAction = temps + suivante.Duree * suivante.Tours + R.D("activite") * (0.5 + hasard.NextDouble()); return; }
            JouerDemande(suivante);
            // un geste en marchant, de temps en temps
            if (suivante.Deplace && hasard.NextDouble() * 100 < R.D("enchaine"))
            {
                List<Anim> gestes = Biblio.Toutes.Where(a => a.Haut && !R.Coupees.Contains(a.Nom)).ToList();
                if (gestes.Count > 0) { Anim g = gestes[hasard.Next(gestes.Count)]; Jouer(g, g.Tours); }
            }
        }

        public void Dire(string texte, double secondes)
        {
            if (!R.O("bulles")) return;
            bulle = texte;
            finBulle = temps + secondes;
        }

        // ------------------------------------------------------------ souris : clic, porté, lancé, bousculé

        void Relacher()
        {
            toile.ReleaseMouseCapture();
            bool portait = etat == Etat.Porte;
            appui = false;
            if (portait)
            {
                // il repart du point où ses pieds se trouvaient, avec l'élan du geste
                Dessin.Os o = Dessin.Calculer(pose, true);
                double bas = Dessin.Points(o).Max(p => p.Y);
                ancre = new Point(curseur.X, curseur.Y + bas * s);
                maison = new Point(ancre.X, maison.Y);
                if (place == 0) R.Mettre("droite", SystemParameters.WorkArea.Right - maison.X);
                Vector elan = R.O("lancer") ? vCurseur * (R.D("force") / 100) : new Vector(0, 0);
                if (elan.Length > 4200) elan *= 4200 / elan.Length;
                if (elan.Length > 700) Sons.Jouer("lance", "sonsSouris");
                Lancer(elan, false, elan.X * 0.5);
                return;
            }
            if (etat != Etat.Anime) return;
            Anim a;
            switch ((int)R.D("clic"))
            {
                case 0: a = Biblio.Salut; break;
                case 1: a = Biblio.Trouver("Saut"); break;
                case 2: a = Biblio.Danse; break;
                case 3: a = Biblio.Trouver("Enchaînement de 3 coups"); break;
                default:
                    List<Anim> libres = Biblio.Toutes.Where(x => !x.Deplace && !R.Coupees.Contains(x.Nom)).ToList();
                    a = libres.Count > 0 ? libres[hasard.Next(libres.Count)] : Biblio.Salut;
                    break;
            }
            JouerDemande(a);
            if (R.O("bulles") && (int)R.D("clic") == 4) Dire(a.Nom, 2.5);
        }

        void Attraper()
        {
            Rompre();
            if (scene != "tour" && scene != "chute" && scene != "tnt") RangerScene();      // la tour s'écroule d'elle-même, la TNT explose quand même
            etat = Etat.Porte;
            support = IntPtr.Zero;
            geste = null; dort = false;
            balance = 0; vBalance = 0;
            voileVise = 1;
            apresSaut = null;
            Sons.Jouer("attrape", "sonsSouris");
            Fondre();
        }

        // Tenu par la tête : le corps pend et se balance derrière le curseur.
        void Porter(double dt)
        {
            double force = R.D("balancier") / 100;
            double visee = Math.Max(-80, Math.Min(80, -vCurseur.X * 0.045 * force));
            vBalance += ((visee - balance) * 60 - vBalance * 7) * dt;
            balance += vBalance * dt;
            var p = new double[I.N];
            p[I.Rot] = balance;
            p[I.Tete] = -balance * 0.25;
            double flotte = 8 * Math.Sin(temps * 3) * force, secousse = Math.Min(30, Math.Abs(vBalance) * 0.08);
            p[I.Ep1] = 22 + balance * 0.5 + flotte + secousse; p[I.Co1] = 14;
            p[I.Ep2] = -18 + balance * 0.5 - flotte - secousse; p[I.Co2] = -10;
            p[I.Ha1] = 12 + balance * 0.55 - flotte * 0.6; p[I.Ge1] = -18 - secousse;
            p[I.Ha2] = -8 + balance * 0.55 + flotte * 0.6; p[I.Ge2] = -30 - secousse;
            pose = Adoucir(p, dt);
        }

        void Lancer(Vector elan, bool voulu, double tournoie)
        {
            etat = Etat.Vol;
            vitesse = elan;
            vrille = tournoie * R.D("tournoie") / 100;
            rotVol = pose[I.Rot];
            rebonds = 0;
            sautVoulu = voulu;
            sautCible = false;
            support = IntPtr.Zero;
            geste = null; enRepos = false; dort = false;
            Fondre();
        }

        // Un curseur qui le traverse à toute vitesse l'envoie valser.
        bool Bouscule()
        {
            if (!R.O("bouscule") || appui || vCurseur.Length < 2600) return false;
            double dx = curseur.X - ancre.X, haut = ancre.Y - curseur.Y;
            if (Math.Abs(dx) > 34 * s || haut < 0 || haut > 125 * s) return false;
            Vector elan = vCurseur * 0.4 * (R.D("force") / 100) + new Vector(0, -320);
            if (elan.Length > 3000) elan *= 3000 / elan.Length;
            Sons.Jouer("aie", "sonsSouris");
            Rompre();
            Lancer(elan, false, elan.X * 0.6);
            Dire("Aïe !", 1.5);
            return true;
        }

        void Voler(double dt)
        {
            double g = 2300 * s * R.D("gravite") / 100;
            Point avant = ancre;
            vitesse.Y += g * dt;
            ancre += vitesse * dt;
            rotVol += vrille * dt;

            double gauche = SystemParameters.VirtualScreenLeft + 20 * s, droite = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 20 * s;
            if (ancre.X < gauche || ancre.X > droite)
            {
                ancre.X = Math.Max(gauche, Math.Min(droite, ancre.X));
                vitesse.X = R.O("murs") ? -vitesse.X * 0.6 : 0;
                vrille = -vrille * 0.6;
            }
            if (ancre.Y < SystemParameters.VirtualScreenTop + 130 * s && vitesse.Y < 0 && !sautVoulu) vitesse.Y *= 0.5;   // pas plus haut que l'écran

            if (mlg && !eauPosee && vitesse.Y > 0 && ancre.Y > baseScene.Y - 190 * s) PoserEau();      // le seau d'eau, au dernier moment
            if (vitesse.Y > 0)
            {
                double sol = SystemParameters.WorkArea.Bottom;
                IntPtr fenetre = IntPtr.Zero;
                if (R.O("fenetres") || apresSaut != null)
                    foreach (Bord bord in Bords())
                        if ((!sautCible || bord.Fenetre == cibleSaut) && avant.Y <= bord.Haut + 1 && ancre.Y >= bord.Haut && bord.Haut < sol - 40
                            && ancre.X > bord.Gauche + 6 && ancre.X < bord.Droite - 6 && BordLibre(bord, ancre.X))
                        { sol = bord.Haut; fenetre = bord.Fenetre; }
                if (ancre.Y >= sol)
                {
                    ancre.Y = sol;
                    double rebond = R.D("rebond") / 100;
                    if (!sautVoulu && vitesse.Y > 520 * s && rebonds < 3 && rebond > 0.03)
                    {
                        vitesse.Y = -vitesse.Y * rebond;
                        vitesse.X *= 1 - R.D("frottement") / 100 * 0.7;
                        vrille *= 0.5;
                        rebonds++;
                        Sons.Jouer("rebond", "sonsSauts");
                    }
                    else { Atterrir(fenetre); return; }
                }
            }

            double[] p;
            tVol += dt;
            if (sautVoulu && styleSaut == 1 && dureeVol > 0)
            {
                p = (double[])Biblio.Groupe.Clone();             // en salto : un tour complet, groupé, le temps du vol
                p[I.Rot] = 360 * Math.Min(1, tVol / dureeVol);
            }
            else if (sautVoulu) p = (double[])(vitesse.Y < 0 ? Biblio.SautMonte : Biblio.SautDescend).Clone();
            else
            {
                p = new double[I.N];
                double agite = Math.Sin(temps * 14);
                p[I.Rot] = Math.IEEERemainder(rotVol, 360);
                p[I.Ep1] = 120 + 40 * agite; p[I.Co1] = 30; p[I.Ep2] = -130 - 40 * agite; p[I.Co2] = -30;
                p[I.Ha1] = 40 + 25 * agite; p[I.Ge1] = -50; p[I.Ha2] = -30 - 25 * agite; p[I.Ge2] = -60;
            }
            pose = Adoucir(p, dt);
        }

        void Atterrir(IntPtr fenetre)
        {
            etat = Etat.Anime;
            support = fenetre;
            Bord bord;
            if (support != IntPtr.Zero && LireBord(support, out bord)) supportX = ancre.X - bord.Gauche; else support = IntPtr.Zero;
            Sons.Jouer("atterrit", "sonsSauts");
            Action suite = apresSaut;
            apresSaut = null;
            if (sautVoulu) Jouer(styleSaut == 2 ? Biblio.Heros : Biblio.Reception, 1, suite);
            else
            {
                face = vitesse.X < 0 ? -1 : 1;
                Jouer(Biblio.SeReleve, 1);
            }
        }

        // ------------------------------------------------------------ dessin

        void Dessiner(DrawingContext dc)
        {
            Color couleur = CouleurDuMoment();
            double epaisseur = R.D("epaisseur") * s, bord = R.D("contour") * s;
            Pen trait = Dessin.Plume(couleur, epaisseur);
            Pen contour = bord > 0.2 ? Dessin.Plume(R.Couleur("contourCouleur"), epaisseur + 2 * bord) : null;
            bool porte = etat == Etat.Porte;
            Dessin.Os o = Dessin.Calculer(pose, porte);
            double ox = largeur / 2, oy = porte ? hauteur * 0.2 : solY;
            int sens = face;
            double taille = s;
            Func<Point, Point> e = p => new Point(ox + sens * p.X * taille, oy + p.Y * taille);

            // zone sensible, presque invisible : les traits seuls seraient trop fins à viser
            Point[] points = Dessin.Points(o).Select(e).ToArray();
            double x0 = points.Min(p => p.X) - 14 * s, x1 = points.Max(p => p.X) + 14 * s, y0 = points.Min(p => p.Y) - (o.Rayon + 12) * s, y1 = points.Max(p => p.Y) + 12 * s;
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)), null, new Rect(x0, y0, x1 - x0, y1 - y0), 20, 20);

            if (R.O("ombre") && etat == Etat.Anime)
            {
                double h = pose[I.Air];
                dc.DrawEllipse(new SolidColorBrush(Color.FromArgb((byte)Math.Max(10, 70 - h * 0.5), 0, 0, 0)), null,
                    new Point(e(o.Hanche).X, solY + 2 * s), Math.Max(8, 24 - h * 0.1) * s, 4 * s);
            }

            string objet = etat == Etat.Anime && R.O("objets") ? (geste != null && geste.Objet != null ? geste.Objet : courante.Objet) : null;
            double phaseObjet = geste != null && geste.Objet != null ? tGeste - Math.Floor(tGeste) : phase;
            if (objet == null && surMusique && etat == Etat.Anime && R.O("objets")) objet = "note";      // il danse sur la musique : des notes s'envolent
            if (etat == Etat.Plane) objet = "elytres";
            else if (mlg && etat == Etat.Vol && R.O("objets")) objet = "seau";

            // traînée : les dernières positions de la main, à l'écran (la fenêtre, elle, se déplace)
            bool dessine = objet == "crayon";
            if (R.O("trainee") || dessine)
            {
                Point main = e(o.M1);
                trace.Add(new Point(main.X + Left, main.Y + Top));
                int longueur = dessine ? 70 : (int)R.D("traineeLongueur");
                while (trace.Count > longueur) trace.RemoveAt(0);
                for (int i = 1; i < trace.Count; i++)
                {
                    byte alpha = (byte)(dessine ? 230 : 200 * i / trace.Count);
                    Color c = dessine ? Color.FromRgb(0xFF, 0xFF, 0xFF) : couleur;
                    dc.DrawLine(Dessin.Plume(Color.FromArgb(alpha, c.R, c.G, c.B), epaisseur * (dessine ? 0.35 : 0.5)),
                        new Point(trace[i - 1].X - Left, trace[i - 1].Y - Top), new Point(trace[i].X - Left, trace[i].Y - Top));
                }
            }
            else if (trace.Count > 0) trace.Clear();

            Dessin.Tracer(dc, o, e, s, contour, trait, TeteCreuse(couleur) ? null : trait.Brush);
            if (objet != null) Dessin.Objet(dc, objet, o, e, s, phaseObjet, couleur, epaisseur);

            if (bulle != null && temps < finBulle)
            {
                FormattedText texte = Dessin.Texte(bulle, 12.5, new SolidColorBrush(Color.FromRgb(0x30, 0x30, 0x30)));
                texte.MaxTextWidth = Math.Max(60, largeur - 30);
                Point tete = e(o.Tete);
                double l = texte.Width + 18, h = texte.Height + 9;
                double bx = Math.Max(4, Math.Min(largeur - l - 4, tete.X - l / 2)), by = Math.Max(4, tete.Y - (o.Rayon + 14) * s - h);
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(0xFA, 0xF9, 0xF5)), new Pen(new SolidColorBrush(Color.FromRgb(0xD8, 0xD4, 0xC8)), 1), new Rect(bx, by, l, h), 9, 9);
                dc.DrawText(texte, new Point(bx + 9, by + 4));
            }
        }

        // ------------------------------------------------------------ menu

        void ConstruireMenu()
        {
            var menu = new ContextMenu();
            var reglages = new MenuItem { Header = "Paramètres…  (" + Biblio.Toutes.Count + " animations)" };
            reglages.Click += (o, e) => OuvrirReglages();
            menu.Items.Add(reglages);

            var couleurs = new MenuItem { Header = "Couleur" };
            foreach (Nuance teinte in Palette)
            {
                int rvb = teinte.Rvb;
                var ligne = new StackPanel { Orientation = Orientation.Horizontal };
                // la pastille montre aussi la tête qu'aura le stickman : anneau ou disque
                ligne.Children.Add(new Border { Width = 14, Height = 14, CornerRadius = new CornerRadius(7), Background = teinte.Creuse ? Brushes.Transparent : new SolidColorBrush(R.Rvb(rvb)), BorderBrush = new SolidColorBrush(R.Rvb(rvb)), BorderThickness = new Thickness(3), Margin = new Thickness(0, 0, 8, 0) });
                ligne.Children.Add(new TextBlock { Text = teinte.Nom + (teinte.Creuse ? "  — tête creuse" : "") });
                var choix = new MenuItem { Header = ligne };
                choix.Click += (o, e) => { R.Mettre(CleCouleur, rvb); R.Mettre("arcenciel", 0); Appliquer(); };
                couleurs.Items.Add(choix);
            }
            var arc = new MenuItem { Header = "Arc-en-ciel" };
            arc.Click += (o, e) => { R.Mettre("arcenciel", 1); Appliquer(); };
            couleurs.Items.Add(arc);
            menu.Items.Add(couleurs);

            var animations = new MenuItem { Header = "Jouer une animation" };
            foreach (string famille in Biblio.Familles)
            {
                List<Anim> liste = Biblio.Toutes.Where(a => a.Famille == famille).ToList();
                var sous = new MenuItem { Header = famille + "  (" + liste.Count + ")" };
                foreach (Anim a in liste)
                {
                    Anim celle = a;
                    var element = new MenuItem { Header = a.Nom };
                    element.Click += (o, e) => { JouerDemande(celle); Dire(celle.Nom, 2.5); };
                    sous.Items.Add(element);
                }
                animations.Items.Add(sous);
            }
            menu.Items.Add(animations);

            // la bande : en ajouter, en renvoyer, aller voir un ami
            var amis = new MenuItem { Header = "Amis" };
            var ajouter = new MenuItem { Header = "Ajouter un stickman" };
            ajouter.Click += (o, e) => { if (Tous.Count < 6) R.Mettre("nombre", Tous.Count); else Dire("On est déjà six !", 2.5); };
            amis.Items.Add(ajouter);
            var renvoyer = new MenuItem { Header = "Renvoyer celui-ci" };
            renvoyer.Click += (o, e) => Renvoyer();
            amis.Items.Add(renvoyer);
            amis.Items.Add(new Separator());
            var voir = new MenuItem { Header = "Aller voir un ami" };
            voir.Click += (o, e) => DemanderDuo(null);
            amis.Items.Add(voir);
            foreach (Duo d in Biblio.Duos)
            {
                Duo celui = d;
                var element = new MenuItem { Header = d.Nom };
                element.Click += (o, e) => DemanderDuo(celui);
                amis.Items.Add(element);
            }
            amis.SubmenuOpened += (o, e) => renvoyer.IsEnabled = place != 0;
            menu.Items.Add(amis);
            var tempo = new MenuItem { Header = "Danser sur la musique" };
            tempo.Click += (o, e) => { if (!musique) Dire("Je n'entends pas de musique…", 2.5); else if (etat == Etat.Anime) { Rompre(); DanserSurLaMusique(); } };
            menu.Items.Add(tempo);

            var fenetre = new MenuItem { Header = "Sauter sur une fenêtre" };
            fenetre.Click += (o, e) => { if (etat == Etat.Anime && !SauterSurFenetre()) Dire("Aucune fenêtre où sauter", 2.5); };
            menu.Items.Add(fenetre);
            var teleport = new MenuItem { Header = "Se téléporter" };
            teleport.Click += (o, e) => Teleporter();
            menu.Items.Add(teleport);
            var farceur = new MenuItem { Header = "Mode farceur : il ferme des fenêtres", IsCheckable = true };
            farceur.Click += (o, e) =>
            {
                R.Mettre("ferme", farceur.IsChecked ? 1 : 0);
                Dire(farceur.IsChecked ? "Hé hé… gare à tes fenêtres !" : "D'accord, je ne touche plus à rien", 3);
            };
            menu.Items.Add(farceur);
            var muet = new MenuItem { Header = "Mode muet (plus aucun bruit)", IsCheckable = true };
            muet.Click += (o, e) =>
            {
                R.Mettre("sons", muet.IsChecked ? 0 : 1);
                Dire(muet.IsChecked ? "Chut… je ne fais plus de bruit" : "Le son est revenu !", 2.5);
                if (!muet.IsChecked) Sons.Jouer("pop", "sons");
            };
            menu.Opened += (o, e) => { farceur.IsChecked = R.O("ferme"); muet.IsChecked = !R.O("sons"); };
            menu.Items.Add(muet);
            var coin = new MenuItem { Header = "Revenir dans le coin" };
            coin.Click += (o, e) =>
            {
                if (etat != Etat.Anime) return;
                Rect zone = SystemParameters.WorkArea;
                Rompre();
                if (place == 0) R.Mettre("droite", 1120);
                maison = new Point(Math.Max(SystemParameters.VirtualScreenLeft + 60, zone.Right - 1120 - 130 * place), zone.Bottom);
                ancre = maison; support = IntPtr.Zero;
                Repos();
            };
            menu.Items.Add(coin);
            var demarrage = new MenuItem { Header = "Lancer au démarrage de Windows", IsCheckable = true, IsChecked = AuDemarrage() };
            demarrage.Click += (o, e) => DefinirDemarrage(demarrage.IsChecked);
            menu.Items.Add(demarrage);
            menu.Items.Add(new Separator());
            var quitter = new MenuItem { Header = "Quitter" };
            quitter.Click += (o, e) => Tous.First(b => b.place == 0).Close();      // fermer le premier ferme toute la bande
            menu.Items.Add(quitter);
            toile.ContextMenu = menu;
        }

        public void OuvrirReglages()
        {
            if (fenetreReglages != null && fenetreReglages.IsVisible) { fenetreReglages.Activate(); return; }
            fenetreReglages = new Parametres(this);
            fenetreReglages.Show();
        }

        const string CleDemarrage = @"Software\Microsoft\Windows\CurrentVersion\Run";

        static bool AuDemarrage()
        {
            using (RegistryKey cle = Registry.CurrentUser.OpenSubKey(CleDemarrage))
                return cle != null && cle.GetValue("MascotteStickman") != null;
        }

        static void DefinirDemarrage(bool actif)
        {
            using (RegistryKey cle = Registry.CurrentUser.OpenSubKey(CleDemarrage, true))
            {
                if (cle == null) return;
                if (actif) cle.SetValue("MascotteStickman", "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"");
                else cle.DeleteValue("MascotteStickman", false);
            }
        }

        // ------------------------------------------------------------ fenêtres-plateformes
        // Le haut d'une fenêtre visible sert de sol : il y saute, s'y promène, voyage avec elle,
        // et retombe si elle disparaît ou passe derrière une autre.

        struct Bord
        {
            public IntPtr Fenetre;
            public double Gauche, Droite, Haut;
        }

        double HauteurCorps { get { return (2 * R.D("jambes") + R.D("torse") + 2 * R.D("tete") + 14) * s; } }

        bool LireBord(IntPtr fenetre, out Bord bord)
        {
            bord = new Bord();
            RECT r;
            int voile;
            if (!IsWindow(fenetre) || !IsWindowVisible(fenetre) || IsIconic(fenetre)) return false;
            if (DwmGetWindowAttribute(fenetre, DWMWA_CLOAKED, out voile, 4) == 0 && voile != 0) return false;
            if (DwmGetWindowAttribute(fenetre, DWMWA_EXTENDED_FRAME_BOUNDS, out r, 16) != 0) return false;
            PresentationSource source = PresentationSource.FromVisual(this);
            Matrix m = source != null ? source.CompositionTarget.TransformFromDevice : Matrix.Identity;
            Point hautGauche = m.Transform(new Point(r.Left, r.Top)), basDroite = m.Transform(new Point(r.Right, r.Bottom));
            if (basDroite.X - hautGauche.X < 200 || basDroite.Y - hautGauche.Y < 80) return false;
            if (hautGauche.Y < SystemParameters.VirtualScreenTop + HauteurCorps) return false;       // pas la place de se tenir dessus
            bord.Fenetre = fenetre; bord.Gauche = hautGauche.X; bord.Droite = basDroite.X; bord.Haut = hautGauche.Y;
            return true;
        }

        List<Bord> Bords()
        {
            var bords = new List<Bord>();
            uint moi = (uint)Process.GetCurrentProcess().Id;
            EnumWindows((fenetre, parametre) =>
            {
                uint processus;
                Bord bord;
                GetWindowThreadProcessId(fenetre, out processus);
                int style = GetWindowLong(fenetre, GWL_EXSTYLE);
                if (processus != moi && (style & (WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE)) == 0
                    && GetWindowTextLength(fenetre) > 0 && LireBord(fenetre, out bord))
                    bords.Add(bord);
                return true;
            }, IntPtr.Zero);
            return bords;
        }

        bool BordLibre(Bord bord, double x)
        {
            PresentationSource source = PresentationSource.FromVisual(this);
            Point pixel = (source != null ? source.CompositionTarget.TransformToDevice : Matrix.Identity).Transform(new Point(x, bord.Haut + 3));
            POINT p;
            p.X = (int)pixel.X; p.Y = (int)pixel.Y;
            IntPtr dessus = GetAncestor(WindowFromPoint(p), GA_ROOT);
            return dessus == bord.Fenetre || Tous.Any(b => b.poignee == dessus);      // un stickman devant ne compte pas
        }

        // Un endroit libre sur le haut d'une autre fenêtre, de préférence proche.
        bool ChercherBord(out Point arrivee, out IntPtr fenetre)
        {
            double sol = SystemParameters.WorkArea.Bottom, meilleur = double.MaxValue;
            arrivee = new Point();
            fenetre = IntPtr.Zero;
            foreach (Bord bord in Bords())
            {
                if (bord.Fenetre == support || bord.Haut > sol - 60) continue;
                double gauche = bord.Gauche + 30, droite = bord.Droite - 30;
                for (int essai = 0; essai < 5; essai++)
                {
                    double x = essai == 0 ? Math.Max(gauche, Math.Min(droite, ancre.X)) : gauche + hasard.NextDouble() * (droite - gauche);
                    if (!BordLibre(bord, x)) continue;
                    double distance = (Math.Abs(x - ancre.X) + 100) * (0.5 + hasard.NextDouble());
                    if (distance < meilleur) { meilleur = distance; arrivee = new Point(x, bord.Haut); fenetre = bord.Fenetre; }
                    break;
                }
            }
            return fenetre != IntPtr.Zero;
        }

        // Disparaît en fondu et réapparaît ailleurs : sur une fenêtre, ou plus loin sur le sol.
        bool Teleporter()
        {
            if (etat != Etat.Anime) return false;
            Rompre();
            Point but;
            IntPtr fenetre;
            if (!ChercherBord(out but, out fenetre) || hasard.Next(3) == 0)
            {
                double gauche = SystemParameters.VirtualScreenLeft + 80 * s, droite = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 80 * s;
                but = new Point(gauche + hasard.NextDouble() * (droite - gauche), SystemParameters.WorkArea.Bottom);
                fenetre = IntPtr.Zero;
            }
            Point ou = but;
            IntPtr sur = fenetre;
            Sons.Jouer("pop", "sonsPouvoirs");
            voileVise = 0;
            Jouer(Biblio.Concentration, 1, () =>
            {
                ancre = ou;
                support = sur;
                Bord bord;
                if (support != IntPtr.Zero && LireBord(support, out bord)) supportX = ancre.X - bord.Gauche;
                else { support = IntPtr.Zero; ancre.Y = SystemParameters.WorkArea.Bottom; }
                voileVise = 1;
                Sons.Jouer("pop", "sonsPouvoirs");
                Jouer(Biblio.Apparition, 1);
            });
            return true;
        }

        // Mode farceur (désactivé par défaut) : il va jusqu'à la croix d'une fenêtre et appuie dessus.
        // C'est l'équivalent exact d'un clic sur la croix : un programme qui a du travail non enregistré
        // peut encore demander confirmation, rien n'est tué de force.
        // filtre : ne viser qu'une fenêtre dont le titre contient ce texte (commande @fermer, pour les essais).
        bool FermerUneFenetre(string filtre)
        {
            if (etat != Etat.Anime) return false;
            Rompre();
            IntPtr active = GetForegroundWindow();
            var choix = new List<Bord>();
            var arrivees = new List<double>();
            foreach (Bord bord in Bords())
            {
                if (bord.Haut > SystemParameters.WorkArea.Bottom - 60 || !BordLibre(bord, bord.Droite - CroixX)) continue;      // croix cachée par une autre fenêtre
                if (filtre != null)
                {
                    var titre = new StringBuilder(300);
                    GetWindowText(bord.Fenetre, titre, titre.Capacity);
                    if (titre.ToString().IndexOf(filtre, StringComparison.OrdinalIgnoreCase) < 0) continue;
                }
                else if (bord.Fenetre == active && !R.O("fermeActive")) continue;      // pas celle qu'on est en train d'utiliser
                for (int essai = 0; essai < 6; essai++)                                 // un endroit dégagé où atterrir, à gauche de la croix
                {
                    double x = Math.Max(bord.Gauche + 30, bord.Droite - 70 - hasard.NextDouble() * 170);
                    if (!BordLibre(bord, x)) continue;
                    choix.Add(bord); arrivees.Add(x);
                    break;
                }
            }
            if (choix.Count == 0) return false;
            int n = hasard.Next(choix.Count);
            for (int i = 0; i < choix.Count; i++) if (choix[i].Fenetre == support) n = i;      // déjà dessus : autant en profiter
            prochaineFermeture = temps + R.D("fermeDelai") * 60 * (0.7 + 0.6 * hasard.NextDouble());
            victime = choix[n].Fenetre;
            croixPressee = false;
            Dire("Hé hé hé…", 2.5);
            if (support == victime) AllerALaCroix();
            else
            {
                apresSaut = AllerALaCroix;
                Bondir(new Point(arrivees[n], choix[n].Haut), victime);
            }
            return true;
        }

        const double CroixX = 23;      // milieu du bouton de fermeture, compté depuis le bord droit de la fenêtre

        void AllerALaCroix()
        {
            Bord bord;
            if (support != victime || !LireBord(victime, out bord)) { Repos(); return; }
            // accroupi, sa main descend à 37 px devant lui : il s'arrête donc juste avant la croix
            AllerVers(bord.Droite - CroixX - 37 * s, Biblio.Marche, () =>
            {
                face = 1;
                Jouer(Biblio.Appuie, 1, () =>
                {
                    if (support != victime || !IsWindow(victime)) { Repos(); return; }
                    Sons.Jouer("pop", "sonsPouvoirs");
                    croixPressee = true;
                    PostMessage(victime, WM_SYSCOMMAND, (IntPtr)SC_CLOSE, IntPtr.Zero);      // comme un vrai clic sur la croix
                    // si la fenêtre se ferme, il n'a plus rien sous les pieds et tombe ; sinon il se relève
                    Jouer(Biblio.Relache, 1, () => { croixPressee = false; Dire("Elle résiste…", 2.5); Repos(); });
                });
            });
        }

        // ------------------------------------------------------------ la bande : plusieurs stickmen
        // Chacun vit sa vie dans sa fenêtre. De temps en temps l'un va en voir un autre : celui-ci l'attend,
        // le premier s'arrête à la bonne distance, et ils jouent ensemble les deux moitiés d'une animation à deux.

        static void AjusterNombre()
        {
            int voulus = (int)R.D("nombre") + 1;
            while (Tous.Count < voulus) new Bonhomme(Tous.Count).Show();
            while (Tous.Count > voulus && Tous.Count > 1)
            {
                Bonhomme dernier = Tous.OrderBy(b => b.place).Last();
                Tous.Remove(dernier);
                dernier.Close();
            }
        }

        void Renvoyer()
        {
            if (place == 0) return;
            // les suivants reculent d'une place en gardant leur couleur ; la sienne va au bout de la file
            double mienne = R.D(CleCouleur);
            int combien = Tous.Count;
            foreach (Bonhomme b in Tous.OrderBy(x => x.place))
                if (b.place > place) { R.Mettre("couleur" + b.place, R.D(b.CleCouleur)); b.place--; }
            R.Mettre("couleur" + combien, mienne);
            Tous.Remove(this);
            R.Mettre("nombre", combien - 2);
            Close();
        }

        // Libre pour une rencontre : il ne dort pas, n'est pas en l'air, et n'est pas au milieu d'une suite
        // d'actions (téléportation, farce, course d'élan…). Une simple animation, il l'interrompt volontiers.
        bool Libre { get { return etat == Etat.Anime && !dort && ami == null && apres == null && voileVise == 1; } }

        void DemanderDuo(Duo lequel)
        {
            if (etat != Etat.Anime) return;
            Rompre();
            if (!Rencontrer(lequel)) Dire(Tous.Count > 1 ? "Personne n'est libre…" : "Je suis tout seul…", 2.5);
        }

        bool Rencontrer(Duo impose)
        {
            if (etat != Etat.Anime || ami != null) return false;
            List<Bonhomme> libres = Tous.Where(b => b != this && b.Libre).ToList();
            if (libres.Count == 0) return false;
            Bonhomme autre = libres.OrderBy(b => (Math.Abs(b.ancre.X - ancre.X) + 200) * (0.5 + hasard.NextDouble())).First();      // plutôt le plus proche
            duo = autre.duo = impose ?? Biblio.Duos[hasard.Next(Biblio.Duos.Count)];
            ami = autre; autre.ami = this;
            enDuo = autre.enDuo = false;
            autre.Attendre();
            Oreille.Trace("duo " + duo.Nom + " : " + place + " va voir " + autre.place + (autre.support == support ? "" : " (en sautant)"));
            Dire("Hé !", 1.5);
            if (autre.support == support) { Approcher(); return true; }

            // pas sur le même perchoir : il saute d'abord le rejoindre
            double cote = ancre.X <= autre.ancre.X ? -1 : 1;
            var but = new Point(autre.ancre.X + cote * (duo.Distance + 50) * s, autre.ancre.Y);
            Bord bord;
            if (autre.support != IntPtr.Zero && LireBord(autre.support, out bord))
            {
                if (but.X < bord.Gauche + 20 || but.X > bord.Droite - 20) but.X = autre.ancre.X - cote * (duo.Distance + 50) * s;
                but.X = Math.Max(bord.Gauche + 20, Math.Min(bord.Droite - 20, but.X));
            }
            apresSaut = Approcher;
            Bondir(but, autre.support);
            return true;
        }

        void Attendre()
        {
            Jouer(Biblio.Repos[0], double.MaxValue);             // debout, tourné vers celui qui arrive
            enRepos = true;
            face = ami.ancre.X >= ancre.X ? 1 : -1;
            finAttente = temps + 25;
            Jouer(Biblio.Salut, 2);
        }

        void Approcher()
        {
            Bonhomme b = ami;
            if (b == null) { Repos(); return; }
            if (b.ami != this || b.etat != Etat.Anime || b.support != support) { Rompre(); Repos(); return; }
            double cote = ancre.X <= b.ancre.X ? -1 : 1, gauche, droite;
            double x = b.ancre.X + cote * duo.Distance * s;
            Limites(out gauche, out droite);
            if (x < gauche || x > droite) x = b.ancre.X - cote * duo.Distance * s;      // pas la place de ce côté : il passe de l'autre
            AllerVers(x, Math.Abs(x - ancre.X) > 380 * s ? Biblio.Course : Biblio.Marche, CommencerDuo);
        }

        void CommencerDuo()
        {
            Bonhomme b = ami;
            Duo d = duo;
            if (b == null || d == null || b.ami != this || b.etat != Etat.Anime || b.support != support) { Rompre(); Repos(); return; }
            face = b.ancre.X >= ancre.X ? 1 : -1;
            b.face = -face;
            b.geste = null; geste = null;
            enDuo = b.enDuo = true;
            bool inverse = d.Hasard && hasard.Next(2) == 0;
            Anim pourMoi = inverse ? d.B : d.A, pourLui = inverse ? d.A : d.B;
            string finMoi = inverse ? d.FinB : d.FinA, finLui = inverse ? d.FinA : d.FinB;
            double tours = 1;
            bool danse = pourMoi == null;
            if (danse)
            {
                List<Anim> danses = Biblio.Toutes.Where(a => a.Famille == "Danses" && !R.Coupees.Contains(a.Nom)).ToList();
                pourMoi = pourLui = danses.Count > 0 ? danses[hasard.Next(danses.Count)] : Biblio.Danse;
                tours = 6;
            }
            Oreille.Trace("duo " + d.Nom + " : commence, écart " + Math.Abs(b.ancre.X - ancre.X).ToString("0") + " pour " + (d.Distance * s).ToString("0"));
            Jouer(pourMoi, tours, () => FinDuo(finMoi));
            b.Jouer(pourLui, tours, () => b.FinDuo(finLui));
            if (danse && musique && R.O("musique")) { Accorder(); b.Accorder(); }
            Dire(d.DitA, 2.5);
            b.Dire(d.DitB, 2.5);
        }

        void FinDuo(string dit)
        {
            ami = null; duo = null; enDuo = false;
            if (dit != null) Dire(dit, 2.5);
            Repos();
        }

        // La rencontre tombe à l'eau (l'un des deux a été attrapé, bousculé, a perdu son perchoir…).
        void Rompre()
        {
            Bonhomme b = ami;
            if (b == null) return;
            Oreille.Trace("duo rompu entre " + place + " et " + b.place);
            ami = null; duo = null; enDuo = false;
            b.ami = null; b.duo = null; b.enDuo = false;
            if (b.etat == Etat.Anime && Tous.Contains(b)) b.Repos();
        }

        // ------------------------------------------------------------ Minecraft : scènes entières
        // Une scène a son décor (tour, TNT, portail, flaque d'eau) et parfois un objet qui vole (perle, fusée) :
        // deux fenêtres que les clics traversent, rangées dès qu'il est dérangé. Le reste se joue avec les
        // animations habituelles, enchaînées par leurs suites.

        Decor decor, volant;
        string scene;                                 // "tour", "chute", "tnt", "perle", "fusee", "portail", "eau" ; null = aucune
        double tScene;                                // secondes depuis le début de l'étape en cours
        int blocsTour, blocsVoulus, blocsVus, typeTour, pasTour, clignote;
        Point baseScene, butScene; IntPtr surScene;
        bool boum, mlg, eauPosee;
        Color feu1, feu2;

        Decor Fenetre(ref Decor d)
        {
            if (d == null) d = new Decor();
            return d;
        }

        void Devant()
        {
            if (Topmost) SetWindowPos(poignee, (IntPtr)(-1), 0, 0, 0, 0, 0x0013);      // il reste devant son décor
        }

        void RangerScene()
        {
            scene = null; boum = false; mlg = false; eauPosee = false;
            if (decor != null) decor.Cacher();
            if (volant != null) volant.Cacher();
        }

        void Speciale(Anim a)
        {
            switch (a.Special)
            {
                case "tour": Batir(false); break;
                case "escalier": Batir(true); break;
                case "elytres": Decoller(); break;
                case "eau": SeauDEau(); break;
                case "perle": PerleDeLEnder(); break;
                case "tnt": Tnt(); break;
                case "fusee": FeuDArtifice(); break;
                case "portail": Portail(); break;
            }
        }

        // Un endroit où aller : un rebord de fenêtre s'il y en a un (une fois sur deux), sinon loin sur le sol.
        void Destination(double mini, double maxi, out Point but, out IntPtr vers)
        {
            if (R.O("fenetres") && hasard.Next(2) == 0 && ChercherBord(out but, out vers)) return;
            double gauche = SystemParameters.VirtualScreenLeft + 80 * s, droite = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 80 * s;
            double x = ancre.X + (hasard.Next(2) == 0 ? -1 : 1) * (mini + hasard.NextDouble() * (maxi - mini)) * s;
            if (x < gauche || x > droite) x = 2 * ancre.X - x;
            but = new Point(Math.Max(gauche, Math.Min(droite, x)), SystemParameters.WorkArea.Bottom);
            vers = IntPtr.Zero;
        }

        // Arrivée quelque part d'un coup (perle, portail) : sur le rebord visé s'il est toujours là, sinon au sol.
        void Arriver(Point ou, IntPtr sur)
        {
            ancre = ou;
            support = sur;
            Bord bord;
            if (support != IntPtr.Zero && LireBord(support, out bord) && Math.Abs(bord.Haut - ancre.Y) < 4) supportX = ancre.X - bord.Gauche;
            else { support = IntPtr.Zero; ancre.Y = SystemParameters.WorkArea.Bottom; }
        }

        // Ce qui, dans une scène, avance tout seul d'image en image.
        void Scene(double dt)
        {
            if (scene == null) return;
            tScene += dt;
            double cote = Biblio.Bloc * s;
            switch (scene)
            {
                case "tour":
                    {
                        // tant qu'il bâtit ou admire la vue, la tour suit ; s'il en est délogé, elle s'écroule et il tombe
                        bool enPose = courante == Biblio.PoseBloc || courante == Biblio.PoseMarche;
                        if (etat == Etat.Anime && (enPose || courante == Biblio.Admire)) Voir(blocsTour + (enPose && tCourante >= 0.55 ? 1 : 0));
                        else
                        {
                            scene = "chute"; tScene = 0;
                            if (etat == Etat.Anime) { styleSaut = 0; Lancer(new Vector(0, 0), true, 0); }
                        }
                        break;
                    }
                case "chute":                                    // un bloc de moins toutes les 80 ms, par le haut
                    if (tScene < 0.08) break;
                    tScene = 0;
                    if (blocsVus > 0) Voir(blocsVus - 1); else RangerScene();
                    break;
                case "perle":
                    {
                        if (etat != Etat.Anime) { RangerScene(); break; }       // dérangé : la perle est perdue
                        double k = Math.Min(1, tScene / 0.9), dx = butScene.X - baseScene.X;
                        double y = baseScene.Y + (butScene.Y - 20 * s - baseScene.Y) * k - 4 * (170 * s + 0.15 * Math.Abs(dx)) * k * (1 - k);
                        volant.Poser(baseScene.X + dx * k - 12 * s, y - 12 * s, 24 * s, 24 * s, Topmost);
                        if (k < 1) break;
                        RangerScene();                                           // la perle touche terre : il s'y retrouve
                        Sons.Jouer("pop", "sonsPouvoirs");
                        Arriver(butScene, surScene);
                        voile = 0; voileVise = 1;
                        Jouer(Biblio.Apparition, 1);
                        break;
                    }
                case "tnt":
                    if (!boum)
                    {
                        int c = (int)(tScene * 5);
                        if (c != clignote) { clignote = c; decor.Redessiner(); }
                        if (tScene < 2.3) break;
                        boum = true; tScene = 0;
                        double r = 115 * s;
                        decor.Poser(baseScene.X - r, baseScene.Y - cote / 2 - r, 2 * r, 2 * r, Topmost);
                        Sons.Jouer("coup", "sonsPouvoirs");
                        foreach (Bonhomme b in Tous.ToArray()) b.Souffle(baseScene);      // toute la bande y passe
                        if (etat == Etat.Anime && courante == Biblio.Oreilles) { Dire("Ouf !", 2); Repos(); }      // assez loin : il s'en tire
                    }
                    else
                    {
                        decor.Redessiner();
                        if (tScene > 0.5) RangerScene();
                    }
                    break;
                case "fusee":
                    if (!boum)
                    {
                        double k = Math.Min(1, tScene / 0.8), e = 1 - (1 - k) * (1 - k);
                        volant.Poser(baseScene.X + (butScene.X - baseScene.X) * e - 16 * s, baseScene.Y + (butScene.Y - baseScene.Y) * e - 24 * s, 32 * s, 48 * s, Topmost);
                        if (k < 1) break;
                        boum = true; tScene = 0;
                        double r = 150 * s;
                        volant.Poser(butScene.X - r, butScene.Y - r, 2 * r, 2 * r, Topmost);
                        Sons.Jouer("pop", "sonsPouvoirs");
                        Dire("Ooooh !", 2.5);
                    }
                    else
                    {
                        volant.Redessiner();
                        if (tScene > 1.2) RangerScene();
                    }
                    break;
            }
        }

        void Voir(int combien)
        {
            if (combien == blocsVus) return;
            blocsVus = combien;
            decor.Redessiner();
        }

        // L'explosion l'envoie valser s'il est resté trop près.
        void Souffle(Point centre)
        {
            double dx = ancre.X - centre.X;
            if (etat != Etat.Anime || Math.Abs(dx) > 270 * s || Math.Abs(ancre.Y - centre.Y) > 220 * s) return;
            Rompre();
            double sens = dx >= 0 ? 1 : -1, force = 1 - Math.Abs(dx) / (400 * s);
            Sons.Jouer("aie", "sonsPouvoirs");
            Lancer(new Vector(sens * 760 * s * force, -900 * s * force), false, sens * 480);
            Dire("Aaaah !", 1.5);
        }

        // Tour : il saute sur place et pose un bloc sous ses pieds, encore et encore. Escalier : chaque bloc
        // est posé un cran plus loin. Arrivé en haut il admire la vue, saute dans le vide, et tout s'écroule.
        void Batir(bool escalier)
        {
            RangerScene();
            double cote = Biblio.Bloc * s;
            int possibles = (int)((ancre.Y - SystemParameters.VirtualScreenTop - HauteurCorps - 30) / cote);      // sans sortir par le haut de l'écran
            blocsVoulus = Math.Min(3 + hasard.Next(escalier ? 3 : 5), possibles);
            if (blocsVoulus < 2) { Repos(); return; }
            pasTour = escalier ? (hasard.Next(2) == 0 ? -1 : 1) : 0;
            if (escalier)
            {
                double bout = ancre.X + pasTour * (blocsVoulus + 1) * cote;
                if (bout < SystemParameters.VirtualScreenLeft + 80 || bout > SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 80) pasTour = -pasTour;
                face = pasTour;
            }
            typeTour = hasard.Next(4) == 0 ? -1 : Blocs.PourBatir[hasard.Next(Blocs.PourBatir.Length)];      // une fois sur quatre : un peu de tout
            blocsTour = blocsVus = 0;
            support = IntPtr.Zero;
            Color couleur = CouleurDuMoment();
            int graine = hasard.Next(100), pas = pasTour, type = typeTour;
            Fenetre(ref decor).Peindre = (dc, l, h) =>
            {
                for (int i = 0; i < blocsVus; i++)
                    for (int j = 0; j <= (pas == 0 ? 0 : i); j++)                // escalier : la marche i est une colonne de i + 1 blocs
                    {
                        double x = pas == 0 ? 0 : pas > 0 ? i * cote : l - (i + 1) * cote;
                        int rang = pas == 0 ? i : j;
                        int bloc = type >= 0 ? type : Blocs.PourBatir[(i * 7 + j * 3 + graine) % Blocs.PourBatir.Length];
                        Blocs.Dessiner(dc, new Rect(x, h - (rang + 1) * cote, cote, cote), bloc, couleur, i + j * 5);
                    }
            };
            double largeur = (pas == 0 ? 1 : blocsVoulus) * cote;
            double gauche = pas == 0 ? ancre.X - cote / 2 : pas > 0 ? ancre.X + cote / 2 : ancre.X - cote / 2 - largeur;
            decor.Poser(gauche, ancre.Y - blocsVoulus * cote, largeur, blocsVoulus * cote, Topmost);
            Devant();
            scene = "tour"; tScene = 0;
            PoserBloc();
        }

        void PoserBloc()
        {
            Jouer(pasTour == 0 ? Biblio.PoseBloc : Biblio.PoseMarche, 1, () =>
            {
                blocsTour++;
                ancre.Y -= Biblio.Bloc * s;
                pose[I.Air] -= Biblio.Bloc;                      // debout sur le nouveau bloc : le même endroit à l'écran
                if (pasTour != 0) { ancre.X += pasTour * Biblio.Bloc * s; pose[I.X] -= Biblio.Bloc; }
                if (blocsTour < blocsVoulus) PoserBloc();
                else Jouer(Biblio.Admire, 1, SauterDeLaTour);
            });
        }

        void SauterDeLaTour()
        {
            double gauche = SystemParameters.VirtualScreenLeft + 60 * s, droite = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 60 * s;
            double x = ancre.X + (pasTour != 0 ? pasTour : hasard.Next(2) == 0 ? -1 : 1) * (140 + hasard.NextDouble() * 220) * s;
            if (x < gauche || x > droite) x = 2 * ancre.X - x;
            scene = "chute"; tScene = 0;
            Dire("Yahou !", 2);
            Bondir(new Point(Math.Max(gauche, Math.Min(droite, x)), SystemParameters.WorkArea.Bottom), IntPtr.Zero);
        }

        // Seau d'eau : un saut immense, et au dernier moment il pose de l'eau sous lui pour amortir la chute.
        void SeauDEau()
        {
            RangerScene();
            double g = 2300 * s * R.D("gravite") / 100;
            double haut = Math.Min(620 * s, ancre.Y - SystemParameters.VirtualScreenTop - HauteurCorps - 40);
            if (haut < 150 * s) { Repos(); return; }
            Dire("Regarde ça !", 2);
            Jouer(Biblio.Elan, 1, () =>
            {
                IntPtr sur = support;
                baseScene = ancre;
                scene = "eau"; tScene = 0;
                mlg = true; eauPosee = false;
                styleSaut = 0; tVol = 0; dureeVol = 0;
                apresSaut = RamasserEau;
                Sons.Jouer("grandsaut", "sonsSauts");
                Lancer(new Vector(0, -Math.Sqrt(2 * g * haut)), true, 0);
                sautCible = true; cibleSaut = sur;                // il retombe là d'où il est parti
            });
        }

        void PoserEau()
        {
            double cote = Biblio.Bloc * s;
            eauPosee = true;
            Color couleur = CouleurDuMoment();
            Fenetre(ref decor).Peindre = (dc, l, h) => Blocs.Dessiner(dc, new Rect(0, 0, l, h), Blocs.Eau, couleur, 0);
            decor.Poser(baseScene.X - cote / 2, baseScene.Y - cote, cote, cote, Topmost);
            Devant();
            Sons.Jouer("pop", "sonsPouvoirs");
        }

        void RamasserEau()
        {
            mlg = false;
            if (!eauPosee) { RangerScene(); Repos(); return; }
            Jouer(Biblio.Ramasse, 1, () => { RangerScene(); Dire("Facile.", 2); Repos(); });
        }

        // Perle de l'Ender : il la lance, et se retrouve là où elle tombe.
        void PerleDeLEnder()
        {
            RangerScene();
            Destination(300, 1100, out butScene, out surScene);
            face = butScene.X >= ancre.X ? 1 : -1;
            Jouer(Biblio.Lance, 1, () =>
            {
                baseScene = new Point(ancre.X + face * 30 * s, ancre.Y - 95 * s);      // la main, bras tendu
                scene = "perle"; tScene = 0;
                Fenetre(ref volant).Peindre = (dc, l, h) =>
                {
                    BitmapSource perle = Textures.Lire("item/ender_pearl");
                    if (perle != null) dc.DrawImage(perle, new Rect(0, 0, l, h));
                    else dc.DrawEllipse(Blocs.Pinceau(Color.FromRgb(0x1E, 0x8C, 0x7A)), new Pen(Blocs.Pinceau(Color.FromRgb(0x0B, 0x4A, 0x44)), 2), new Point(l / 2, h / 2), l / 2 - 2, h / 2 - 2);
                };
                Sons.Jouer("lance", "sonsPouvoirs");
                Jouer(Biblio.Repos[0], double.MaxValue);         // il la suit des yeux
            });
        }

        // TNT : il la pose, l'allume, court se mettre à l'abri… pas toujours assez loin.
        void Tnt()
        {
            RangerScene();
            double cote = Biblio.Bloc * s;
            Jouer(Biblio.PoseDevant, 1, () =>
            {
                baseScene = new Point(ancre.X + face * (cote / 2 + 34 * s), ancre.Y);
                scene = "tnt"; tScene = 0; boum = false; clignote = 0;
                Color couleur = CouleurDuMoment();
                Fenetre(ref decor).Peindre = (dc, l, h) =>
                {
                    if (!boum)
                    {
                        Blocs.Dessiner(dc, new Rect(0, 0, l, h), Blocs.Tnt, couleur, 0);
                        if (clignote % 2 == 1) dc.DrawRectangle(Blocs.Pinceau(Color.FromArgb(150, 255, 255, 255)), null, new Rect(0, 0, l, h));      // elle clignote, comme dans le jeu
                        return;
                    }
                    double k = Math.Min(1, tScene / 0.5), r = l / 2;      // l'explosion : un éclair, des boules de fumée
                    byte alpha = (byte)(255 * (1 - k));
                    dc.DrawEllipse(Blocs.Pinceau(Color.FromArgb(alpha, 255, 200, 80)), null, new Point(r, r), r * (0.35 + 0.6 * k), r * (0.35 + 0.6 * k));
                    for (int i = 0; i < 7; i++)
                    {
                        double a = i * 0.9 + 0.4, d = r * (0.2 + 0.55 * k);
                        byte gris = (byte)(230 - i * 18);
                        dc.DrawEllipse(Blocs.Pinceau(Color.FromArgb(alpha, gris, gris, gris)), null, new Point(r + Math.Cos(a) * d, r + Math.Sin(a) * d), r * (0.2 + 0.1 * (i % 3)), r * (0.2 + 0.1 * (i % 3)));
                    }
                    dc.DrawEllipse(Blocs.Pinceau(Color.FromArgb(alpha, 255, 255, 255)), null, new Point(r, r), r * 0.3 * (1 - k), r * 0.3 * (1 - k));
                };
                decor.Poser(baseScene.X - cote / 2, baseScene.Y - cote, cote, cote, Topmost);
                Devant();
                Sons.Jouer("pop", "sonsPouvoirs");
                double abri = ancre.X - face * (150 + hasard.Next(190)) * s;      // parfois trop près…
                AllerVers(abri, Biblio.Course, () =>
                {
                    face = baseScene.X >= ancre.X ? 1 : -1;
                    Jouer(Biblio.Oreilles, 12);                  // à l'abri, les mains sur les oreilles
                });
            });
        }

        // Feu d'artifice : une fusée part de ses pieds et éclate en couleurs au-dessus de lui.
        void FeuDArtifice()
        {
            RangerScene();
            Jouer(Biblio.PoseDevant, 1, () =>
            {
                baseScene = new Point(ancre.X + face * 36 * s, ancre.Y - 24 * s);
                double haut = Math.Max(130 * s, Math.Min(430 * s, baseScene.Y - SystemParameters.VirtualScreenTop - 170 * s));
                butScene = new Point(baseScene.X + face * 30 * s, baseScene.Y - haut);
                scene = "fusee"; tScene = 0; boum = false;
                var vives = new[] { 0xFF3B30, 0xFFD60A, 0x34C759, 0x32ADE6, 0xFF2D95, 0xAF52DE, 0xFF9500, 0xFFFFFF };
                feu1 = R.Rvb(vives[hasard.Next(vives.Length)]);
                feu2 = R.Rvb(vives[hasard.Next(vives.Length)]);
                Fenetre(ref volant).Peindre = (dc, l, h) =>
                {
                    if (!boum)
                    {
                        BitmapSource fusee = Textures.Lire("item/firework_rocket");
                        if (fusee != null) dc.DrawImage(fusee, new Rect(0, 0, l, l));
                        else dc.DrawRectangle(Blocs.Pinceau(feu1), null, new Rect(l * 0.35, 0, l * 0.3, l * 0.8));
                        for (int i = 0; i < 4; i++)                  // la traînée d'étincelles
                            dc.DrawRectangle(Blocs.Pinceau(Color.FromArgb((byte)(220 - i * 50), 255, 220, 120)), null, new Rect(l / 2 - 2 * s + (i % 2 * 4 - 2) * s, l * 0.85 + i * 5 * s, 4 * s, 4 * s));
                        return;
                    }
                    double k = Math.Min(1, tScene / 1.2), r = l / 2, e = 1 - (1 - k) * (1 - k) * (1 - k);
                    byte alpha = (byte)(255 * (1 - k * k));
                    for (int i = 0; i < 36; i++)                     // deux couronnes d'étincelles carrées, qui retombent un peu
                    {
                        double a = i * Math.PI / 9 + (i >= 18 ? 0.17 : 0), d = r * e * (i >= 18 ? 0.55 : 0.92);
                        Color c = i >= 18 ? feu2 : feu1;
                        dc.DrawRectangle(Blocs.Pinceau(Color.FromArgb(alpha, c.R, c.G, c.B)), null, new Rect(r + Math.Cos(a) * d - 3 * s, r + Math.Sin(a) * d + 30 * s * k * k - 3 * s, 6 * s, 6 * s));
                    }
                };
                Sons.Jouer("swish", "sonsPouvoirs");
                Jouer(Biblio.Admire, 2);
            });
        }

        // Portail du Nether : il entre par un portail et ressort par un autre, ailleurs sur l'écran.
        void Portail()
        {
            RangerScene();
            double c = 32 * s;                                   // cadre de 4 blocs sur 5, deux fois la taille de leur texture
            Destination(350, 1200, out butScene, out surScene);
            Color couleur = CouleurDuMoment();
            Fenetre(ref decor).Peindre = (dc, l, h) =>
            {
                for (int i = 0; i < 4; i++)
                    for (int j = 0; j < 5; j++)
                        Blocs.Dessiner(dc, new Rect(i * c, j * c, c, c), i == 0 || i == 3 || j == 0 || j == 4 ? Blocs.Obsidienne : Blocs.Portail, couleur, i + j * 4);
            };
            double gauche, droite;
            Limites(out gauche, out droite);
            double entree = Math.Max(gauche, Math.Min(droite, ancre.X + face * 130 * s));
            decor.Poser(entree - 2 * c, ancre.Y - 5 * c, 4 * c, 5 * c, Topmost);
            Devant();
            scene = "portail"; tScene = 0;
            Sons.Jouer("pop", "sonsPouvoirs");
            AllerVers(entree, Biblio.Marche, () =>
            {
                voileVise = 0;
                Sons.Jouer("energie", "sonsPouvoirs");
                Jouer(Biblio.Concentration, 1, () =>
                {
                    Arriver(butScene, surScene);                 // de l'autre côté
                    decor.Poser(ancre.X - 2 * c, ancre.Y - 5 * c, 4 * c, 5 * c, Topmost);
                    Devant();
                    voileVise = 1;
                    Sons.Jouer("pop", "sonsPouvoirs");
                    Jouer(Biblio.Apparition, 1, () => AllerVers(ancre.X + face * 120 * s, Biblio.Marche, () => { RangerScene(); Repos(); }));
                });
            });
        }

        // Élytres : un élan, puis il s'envole le long d'une grande courbe jusqu'à un rebord de fenêtre ou loin sur le sol.
        Point e0, e1, e2, e3; double tPlane, dureePlane; IntPtr cibleElytres;

        void Decoller()
        {
            RangerScene();
            Point arrivee;
            IntPtr sur;
            Destination(500, 1400, out arrivee, out sur);
            face = arrivee.X >= ancre.X ? 1 : -1;
            Dire("Élytres !", 2);
            Jouer(Biblio.Elan, 1, () =>
            {
                double d = Math.Abs(arrivee.X - ancre.X);
                double haut = Math.Min(200 * s + d * 0.15, Math.Min(ancre.Y, arrivee.Y) - SystemParameters.VirtualScreenTop - 170 * s);
                haut = Math.Max(70 * s, haut);
                double sommet = Math.Min(ancre.Y, arrivee.Y) - haut;
                e0 = ancre; e3 = arrivee;
                e1 = new Point(e0.X + (e3.X - e0.X) * 0.12, sommet - haut * 0.4);      // montée raide, puis long plané
                e2 = new Point(e0.X + (e3.X - e0.X) * 0.7, sommet + haut * 0.1);
                dureePlane = Math.Max(1.6, (d + 2 * haut) / (480 * s));
                tPlane = 0;
                cibleElytres = sur;
                etat = Etat.Plane;
                support = IntPtr.Zero;
                geste = null; enRepos = false;
                Sons.Jouer("grandsaut", "sonsSauts");
                Fondre();
            });
        }

        void Planer(double dt)
        {
            tPlane += dt / dureePlane;
            double t = Math.Min(1, tPlane), u = 1 - t;
            ancre = new Point(u * u * u * e0.X + 3 * u * u * t * e1.X + 3 * u * t * t * e2.X + t * t * t * e3.X,
                              u * u * u * e0.Y + 3 * u * u * t * e1.Y + 3 * u * t * t * e2.Y + t * t * t * e3.Y);
            Vector v = 3 * u * u * (e1 - e0) + 6 * u * t * (e2 - e1) + 3 * t * t * (e3 - e2);
            if (Math.Abs(v.X) > 1) face = v.X > 0 ? 1 : -1;
            double pente = Math.Atan2(-v.Y, Math.Abs(v.X)) * 180 / Math.PI;      // positif : il monte
            var p = (double[])Biblio.PoseElytres.Clone();
            p[I.Rot] = 90 - pente;                               // le corps suit la trajectoire, tête la première
            if (t > 0.85)                                        // il se redresse pour se poser
            {
                double k = (t - 0.85) / 0.15;
                p[I.Rot] += (20 - p[I.Rot]) * k;
                p[I.Ha1] = 4 + 40 * k; p[I.Ha2] = -4 + 30 * k; p[I.Ge1] = -30 * k; p[I.Ge2] = -40 * k;
            }
            pose = Adoucir(p, dt);
            if (tPlane < 1) return;

            ancre = e3;
            sautVoulu = true;
            sautCible = false;
            styleSaut = 0;
            Bord bord;
            if (cibleElytres != IntPtr.Zero && !(LireBord(cibleElytres, out bord) && Math.Abs(bord.Haut - e3.Y) < 4 && BordLibre(bord, e3.X)))
            {
                Lancer(new Vector(v.X * 0.3, 0), true, 0);       // la fenêtre visée a bougé ou disparu : il tombe
                return;
            }
            Atterrir(cibleElytres);
        }

        // ------------------------------------------------------------ musique
        // L'oreille annonce la musique, son tempo et chaque temps. Quand il en a envie, il choisit une danse
        // et la cale dessus : deux temps par cycle (un seul si la musique est lente), les accents sur les temps.

        bool DanserSurLaMusique()
        {
            if (etat != Etat.Anime) return false;
            List<Anim> danses = Biblio.Toutes.Where(a => a.Famille == "Danses" && !R.Coupees.Contains(a.Nom)).ToList();
            if (danses.Count == 0) return false;
            Jouer(danses[hasard.Next(danses.Count)], 1);
            Accorder();
            toursCourante = Math.Max(3, Math.Round((8 + 4 * hasard.Next(4)) * periodeMusique / dureeForcee));      // de 8 à 20 temps
            if (hasard.Next(3) == 0) Dire("♪ ♫", 2);
            return true;
        }

        void Accorder()
        {
            surMusique = true;
            dureeForcee = periodeMusique * (periodeMusique > 0.75 ? 1 : 2);
        }

        static void MusiqueChange(bool active, double periode)
        {
            bool debut = active && !musique;
            musique = active;
            periodeMusique = periode;
            foreach (Bonhomme b in Tous)
            {
                if (b.surMusique)
                {
                    if (active) b.Accorder();
                    else b.toursCourante = Math.Min(b.toursCourante, Math.Ceiling(b.tCourante));      // plus de musique : il finit son mouvement
                }
                else if (debut && b.enRepos && b.ami == null) b.prochaineAction = Math.Min(b.prochaineAction, b.temps + 1 + 3 * b.hasard.NextDouble());
            }
        }

        static void DemiTemps(bool fort)
        {
            if (!fort) return;
            foreach (Bonhomme b in Tous) if (b.surMusique && b.etat == Etat.Anime) b.Caler();
        }

        // Sur chaque temps, la danse est ramenée en douceur vers son accent le plus proche (phases 0,25 et 0,75).
        void Caler()
        {
            double parCycle = Math.Max(1, Math.Round(dureeForcee / periodeMusique));
            double x = (tCourante - 0.25) * parCycle, ecart = (Math.Round(x) - x) / parCycle;
            tCourante = Math.Max(0, tCourante + 0.4 * ecart);
            Oreille.Trace("stickman " + place + " : écart au temps " + (ecart * dureeForcee * 1000).ToString("0") + " ms");
        }

        bool SauterSurFenetre()
        {
            Rompre();
            double sol = SystemParameters.WorkArea.Bottom;
            Point arrivee;
            IntPtr vers;
            if (!ChercherBord(out arrivee, out vers))
            {
                if (support == IntPtr.Zero) return false;
                arrivee = new Point(ancre.X + (hasard.Next(2) == 0 ? -1 : 1) * (60 + hasard.NextDouble() * 160), sol);       // sinon il redescend
                vers = IntPtr.Zero;
            }
            Point but = arrivee;
            IntPtr sur = vers;
            if (Math.Abs(but.X - ancre.X) > 1100 && support == IntPtr.Zero)
            {
                AllerVers(but.X - Math.Sign(but.X - ancre.X) * 450, Biblio.Course, () => Bondir(but, sur));    // trop loin : il court d'abord
                return true;
            }
            Bondir(but, sur);
            return true;
        }

        // Saut calculé pour retomber exactement au point visé.
        // vers : la fenêtre visée (zéro = le sol). En vol, il ne se pose sur rien d'autre, sans quoi il
        // retomberait souvent sur la fenêtre qu'il vient de quitter.
        void Bondir(Point arrivee, IntPtr vers)
        {
            cibleSaut = vers;
            double g = 2300 * s * R.D("gravite") / 100;
            double dx = arrivee.X - ancre.X, dy = arrivee.Y - ancre.Y;
            double duree = Math.Max(0.5, Math.Min(1.3, 0.45 + (Math.Abs(dx) + Math.Abs(dy)) / 1500)) * Math.Sqrt(100 / R.D("gravite"));
            if (dy < 0) duree = Math.Max(duree, Math.Sqrt(-2 * dy / g) * 1.35);                 // assez long pour arriver par le dessus
            if (dx != 0) face = dx > 0 ? 1 : -1;
            int reglage = (int)R.D("styleSaut");
            styleSaut = reglage == 0 ? hasard.Next(3) : reglage - 1;
            tVol = 0;
            dureeVol = duree;
            Sons.Jouer(Math.Abs(dx) + Math.Abs(dy) > 700 ? "grandsaut" : "saut", "sonsSauts");
            Lancer(new Vector(dx / duree, dy / duree - 0.5 * g * duree), true, 0);
            sautCible = true;
        }

        bool SupportPerdu()
        {
            if (support == IntPtr.Zero) return false;
            Bord bord;
            double x = 0;
            bool ok = LireBord(support, out bord);
            if (ok) { x = bord.Gauche + supportX; ok = x > bord.Gauche + 4 && x < bord.Droite - 4 && BordLibre(bord, x); }
            if (!ok)                                                                                 // plus rien sous les pieds : il tombe
            {
                styleSaut = 0;
                if (croixPressee && support == victime) Dire("Oups !", 2.5);                         // il vient de fermer la fenêtre sous ses pieds
                croixPressee = false;
                Rompre();
                Sons.Jouer("glisse", "sonsSauts");
                Lancer(new Vector(0, 0), true, 0);
                return true;
            }
            ancre = new Point(x, bord.Haut);                                                         // la fenêtre a bougé : il voyage avec
            return false;
        }

        static double SecondesSansSaisie()
        {
            var info = new LASTINPUTINFO { Taille = 8 };
            return GetLastInputInfo(ref info) ? unchecked((uint)Environment.TickCount - info.Dernier) / 1000.0 : 0;
        }

        // ------------------------------------------------------------ Win32

        const int GWL_EXSTYLE = -20, WS_EX_TOOLWINDOW = 0x80, WS_EX_TRANSPARENT = 0x20, WS_EX_NOACTIVATE = 0x08000000;
        const int DWMWA_EXTENDED_FRAME_BOUNDS = 9, DWMWA_CLOAKED = 14;
        const uint GA_ROOT = 2;
        const int WM_SYSCOMMAND = 0x0112, SC_CLOSE = 0xF060;

        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr fenetre, IntPtr apres, int x, int y, int l, int h, uint drapeaux);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr fenetre, StringBuilder texte, int taille);
        [DllImport("user32.dll")] static extern bool PostMessage(IntPtr fenetre, int message, IntPtr w, IntPtr l);

        [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] struct LASTINPUTINFO { public uint Taille, Dernier; }
        delegate bool RappelFenetre(IntPtr fenetre, IntPtr parametre);

        [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT point);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr fenetre, int index);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr fenetre, int index, int valeur);
        [DllImport("user32.dll")] static extern bool EnumWindows(RappelFenetre rappel, IntPtr parametre);
        [DllImport("user32.dll")] static extern bool IsWindow(IntPtr fenetre);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr fenetre);
        [DllImport("user32.dll")] static extern bool IsIconic(IntPtr fenetre);
        [DllImport("user32.dll")] static extern int GetWindowTextLength(IntPtr fenetre);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr fenetre, out uint processus);
        [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(POINT point);
        [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr fenetre, uint drapeau);
        [DllImport("user32.dll")] static extern bool GetLastInputInfo(ref LASTINPUTINFO info);
        [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr fenetre, int attribut, out RECT valeur, int taille);
        [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr fenetre, int attribut, out int valeur, int taille);
    }

    // ============================================================ fenêtre des réglages
    // Fabriquée à partir de la liste des réglages : un onglet par catégorie, une ligne par réglage,
    // plus un onglet qui liste toutes les animations (à cocher, à essayer).

    sealed class Parametres : Window
    {
        readonly Bonhomme bonhomme;
        ListBox liste;
        TextBox recherche;
        TextBlock compte;

        public Parametres(Bonhomme proprietaire)
        {
            bonhomme = proprietaire;
            int reglages = R.Tous.Count(p => p.Cat != "");
            Title = "Stickman — " + Biblio.Toutes.Count + " animations, " + (reglages + Biblio.Toutes.Count) + " réglages";
            Width = 640; Height = 700;
            Topmost = true;                                      // reste visible pendant qu'on règle : on voit l'effet en direct
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            var onglets = new TabControl { Margin = new Thickness(6) };
            foreach (string categorie in R.Tous.Select(p => p.Cat).Where(c => c != "").Distinct())
            {
                var pile = new StackPanel { Margin = new Thickness(12) };
                if (categorie == "Familles") pile.Children.Add(new TextBlock { Text = "À quelle fréquence il choisit chaque famille d'animations (0 = jamais).", Margin = new Thickness(0, 0, 0, 10), TextWrapping = TextWrapping.Wrap });
                foreach (Param p in R.Tous.Where(x => x.Cat == categorie)) pile.Children.Add(Ligne(p));
                var remise = new Button { Content = "Remettre cet onglet à zéro", HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 14, 0, 0) };
                string cat = categorie;
                remise.Click += (o, e) =>
                {
                    foreach (Param p in R.Tous.Where(x => x.Cat == cat)) R.Mettre(p.Cle, p.Defaut);
                    bonhomme.Appliquer();
                    Close();
                    bonhomme.OuvrirReglages();               // rouverte pour que les curseurs reprennent leurs valeurs
                };
                pile.Children.Add(remise);
                onglets.Items.Add(new TabItem { Header = categorie, Content = new ScrollViewer { Content = pile, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } });
            }
            onglets.Items.Add(new TabItem { Header = "Animations (" + Biblio.Toutes.Count + ")", Content = OngletAnimations() });
            Content = onglets;
        }

        UIElement Ligne(Param p)
        {
            var grille = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            grille.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
            grille.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grille.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
            var nom = new TextBlock { Text = p.Nom, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
            grille.Children.Add(nom);
            UIElement controle;
            if (p.Type == 'b')
            {
                var coche = new CheckBox { IsChecked = p.V != 0, VerticalAlignment = VerticalAlignment.Center };
                coche.Click += (o, e) => { R.Mettre(p.Cle, coche.IsChecked == true ? 1 : 0); bonhomme.Appliquer(); };
                controle = coche;
            }
            else if (p.Type == 'c')
            {
                var choix = new ComboBox { ItemsSource = p.Choix, SelectedIndex = (int)p.V };
                choix.SelectionChanged += (o, e) => R.Mettre(p.Cle, choix.SelectedIndex);
                controle = choix;
            }
            else if (p.Type == 'k') controle = Nuancier(p);
            else
            {
                var valeur = new TextBlock { Text = Ecrire(p), VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right };
                Grid.SetColumn(valeur, 2);
                grille.Children.Add(valeur);
                var curseur = new Slider { Minimum = p.Min, Maximum = p.Max, Value = p.V, VerticalAlignment = VerticalAlignment.Center };
                curseur.ValueChanged += (o, e) => { R.Mettre(p.Cle, p.Max - p.Min > 20 ? Math.Round(curseur.Value) : Math.Round(curseur.Value, 2)); valeur.Text = Ecrire(p); bonhomme.Appliquer(); };
                controle = curseur;
            }
            Grid.SetColumn(controle, 1);
            grille.Children.Add(controle);
            return grille;
        }

        static string Ecrire(Param p) { return p.V.ToString(p.Max - p.Min > 20 ? "0" : "0.##", CultureInfo.CurrentCulture); }

        // Couleur : les teintes toutes prêtes, puis trois curseurs pour n'importe quelle autre.
        UIElement Nuancier(Param p)
        {
            var pile = new StackPanel();
            var pastilles = new WrapPanel();
            var curseurs = new Slider[3];
            var apercu = new Border { Width = 34, Height = 34, CornerRadius = new CornerRadius(17), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), Background = new SolidColorBrush(R.Couleur(p.Cle)), Margin = new Thickness(0, 0, 10, 0) };
            Action<int> choisir = rvb =>
            {
                R.Mettre(p.Cle, rvb);
                if (p.Cle == "couleur") R.Mettre("arcenciel", 0);
                apercu.Background = new SolidColorBrush(R.Rvb(rvb));
                bonhomme.Appliquer();
            };
            foreach (Bonhomme.Nuance teinte in Bonhomme.Palette)
            {
                int rvb = teinte.Rvb;
                var pastille = new Button { Width = 24, Height = 24, Margin = new Thickness(2), Background = new SolidColorBrush(R.Rvb(rvb)), ToolTip = teinte.Nom + (teinte.Creuse ? " (tête creuse)" : " (tête pleine)") };
                pastille.Click += (o, e) => { choisir(rvb); for (int i = 0; i < 3; i++) curseurs[i].Value = (rvb >> (16 - 8 * i)) & 255; };
                pastilles.Children.Add(pastille);
            }
            pile.Children.Add(pastilles);
            var ligne = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            ligne.Children.Add(apercu);
            var trois = new StackPanel { Width = 190 };
            for (int i = 0; i < 3; i++)
            {
                curseurs[i] = new Slider { Minimum = 0, Maximum = 255, Value = ((int)p.V >> (16 - 8 * i)) & 255, Foreground = i == 0 ? Brushes.Red : i == 1 ? Brushes.Green : Brushes.Blue, ToolTip = i == 0 ? "Rouge" : i == 1 ? "Vert" : "Bleu" };
                curseurs[i].ValueChanged += (o, e) => choisir(((int)curseurs[0].Value << 16) | ((int)curseurs[1].Value << 8) | (int)curseurs[2].Value);
                trois.Children.Add(curseurs[i]);
            }
            ligne.Children.Add(trois);
            pile.Children.Add(ligne);
            return pile;
        }

        UIElement OngletAnimations()
        {
            var panneau = new DockPanel { Margin = new Thickness(10) };
            var haut = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            recherche = new TextBox { Width = 190, VerticalContentAlignment = VerticalAlignment.Center, ToolTip = "Chercher par nom ou par famille" };
            recherche.TextChanged += (o, e) => Remplir();
            haut.Children.Add(new TextBlock { Text = "Chercher : ", VerticalAlignment = VerticalAlignment.Center });
            haut.Children.Add(recherche);
            haut.Children.Add(Bouton("▶ Jouer", () => { var c = liste.SelectedItem as CheckBox; if (c != null) Essayer((Anim)c.Tag); }));
            haut.Children.Add(Bouton("Tout cocher", () => Cocher(true)));
            haut.Children.Add(Bouton("Tout décocher", () => Cocher(false)));
            DockPanel.SetDock(haut, Dock.Top);
            panneau.Children.Add(haut);
            compte = new TextBlock { Margin = new Thickness(0, 6, 0, 0), Foreground = Brushes.Gray };
            DockPanel.SetDock(compte, Dock.Bottom);
            panneau.Children.Add(compte);
            liste = new ListBox();
            liste.MouseDoubleClick += (o, e) => { var c = liste.SelectedItem as CheckBox; if (c != null) Essayer((Anim)c.Tag); };
            panneau.Children.Add(liste);
            Remplir();
            return panneau;
        }

        Button Bouton(string texte, Action action)
        {
            var bouton = new Button { Content = texte, Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(10, 3, 10, 3) };
            bouton.Click += (o, e) => action();
            return bouton;
        }

        void Essayer(Anim a)
        {
            if (a.Special != null) bonhomme.Demander(a); else bonhomme.Jouer(a, Math.Max(1, a.Tours));
            bonhomme.Dire(a.Nom, 2.5);
        }

        void Remplir()
        {
            liste.Items.Clear();
            string filtre = recherche.Text.Trim();
            foreach (Anim a in Biblio.Toutes)
            {
                if (filtre != "" && (a.Famille + " " + a.Nom).IndexOf(filtre, StringComparison.CurrentCultureIgnoreCase) < 0) continue;
                Anim celle = a;
                var coche = new CheckBox { Content = a.Famille + "  —  " + a.Nom, IsChecked = !R.Coupees.Contains(a.Nom), Tag = a, Margin = new Thickness(2) };
                coche.Click += (o, e) =>
                {
                    if (coche.IsChecked == true) R.Coupees.Remove(celle.Nom); else R.Coupees.Add(celle.Nom);
                    R.Sale = true;
                    Compter();
                };
                liste.Items.Add(coche);
            }
            Compter();
        }

        void Cocher(bool oui)
        {
            foreach (CheckBox coche in liste.Items)
            {
                coche.IsChecked = oui;
                string nom = ((Anim)coche.Tag).Nom;
                if (oui) R.Coupees.Remove(nom); else R.Coupees.Add(nom);
            }
            R.Sale = true;
            Compter();
        }

        void Compter()
        {
            compte.Text = liste.Items.Count + " affichées — " + (Biblio.Toutes.Count - R.Coupees.Count) + " animations actives sur " + Biblio.Toutes.Count
                + ". Double-clic pour en essayer une ; décochée, elle n'est plus choisie au hasard.";
        }
    }
}
