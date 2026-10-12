// Le moteur des stickmen du fond d'écran : leurs animations, ce qu'ils décident de faire, et la façon de les
// dessiner. Rien ici ne dépend d'Android : tout passe par l'interface Toile, ce qui permet d'essayer le moteur
// sur un PC (voir essai/Essai.java) avant de l'installer sur un téléphone.
//
// Les animations viennent de assets/animations.bin, écrit par « MascotteStickman.exe --android » : ce sont les
// mêmes que celles de la version Windows, en poses de 13 nombres que l'on relie ici entre elles.
package io.github.minecraft2048.stickman;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;
import java.util.Random;

public final class Moteur {
    // Une pose : décalage, hauteur en l'air, rotation du corps, puis les angles (degrés) du torse, de la tête,
    // des épaules et des coudes, des hanches et des genoux.
    static final int IX = 0, AIR = 1, ROT = 2, TORSE = 3, TETE = 4, EP1 = 5, CO1 = 6, EP2 = 7, CO2 = 8, HA1 = 9, GE1 = 10, HA2 = 11, GE2 = 12, N = 13;
    static final float L_TORSE = 38, L_BRAS = 19, L_JAMBE = 22, R_TETE = 13;
    static final float[] NEUTRE = { 0, 0, 0, 0, 0, 10, 12, -10, 12, 6, 0, -6, 0 };
    // couleur, et 1 si la tête est un simple anneau (comme l'orange)
    static final int[][] PALETTE = {
        { 0xFFFF8A1A, 1 }, { 0xFFE53935, 0 }, { 0xFF1E88E5, 0 }, { 0xFF43A047, 0 }, { 0xFFFDD835, 0 }, { 0xFF8E24AA, 0 }, { 0xFFEC407A, 0 }, { 0xFF00BCD4, 0 },
        { 0xFFF5F5F5, 0 }, { 0xFF111111, 1 }, { 0xFFB71C1C, 1 }, { 0xFF26A69A, 0 }, { 0xFFFFC107, 0 }, { 0xFFC6FF00, 0 }, { 0xFF3949AB, 0 }, { 0xFF9E9E9E, 0 },
    };
    // À quelle fréquence il choisit chaque famille d'animations.
    static final String[] FAMILLES = { "Déplacements", "Danses", "Gestes", "Combat", "Acrobaties", "Sport", "Quotidien", "Émotions" };
    static final int[] POIDS = { 50, 30, 50, 30, 50, 50, 50, 50 };
    // les points du squelette, deux nombres chacun
    static final int HANCHE = 0, COU = 2, CRANE = 4, C1 = 6, M1 = 8, C2 = 10, M2 = 12, G1 = 14, P1 = 16, G2 = 18, P2 = 20, POINTS = 22;

    // Ce sur quoi on dessine : un Canvas d'Android, ou une image sur PC pour les essais. Couleurs en ARGB.
    public interface Toile {
        void trait(float[] points, int nombre, float epaisseur, int couleur);      // ligne brisée, bouts et coudes ronds
        void disque(float x, float y, float rayon, int couleur);
        void anneau(float x, float y, float rayon, float epaisseur, int couleur);
        void ovale(float x, float y, float rayonX, float rayonY, int couleur);
        void pastille(float x, float y, float largeur, float hauteur, int couleur);  // rectangle aux bouts ronds, centré
        float largeurTexte(String texte, float taille, boolean gras);
        void texte(String texte, float x, float y, float taille, boolean gras, int couleur);   // centré sur (x, y)
    }

    public static final class Anim {
        public String nom, anglais, famille, objet;
        public float duree, vitesse;
        public int tours, nombre;                 // nombre : combien de poses
        public boolean haut, deplace;             // haut du corps seulement ; déplacement
        short[] poses;

        // La pose à l'instant t (0 à 1 dans son cycle).
        void pose(float t, float[] p) {
            int n = nombre - 1;
            float x = Math.max(0, Math.min(1, t)) * n;
            int i = Math.min(n - 1, (int) x), a = i * N, b = a + N;
            float k = x - i;
            for (int j = 0; j < N; j++) p[j] = poses[a + j] + (poses[b + j] - poses[a + j]) * k;
        }
    }

    public static final class Biblio {
        public Anim[] repos, toutes;
        public Anim reception, releve, marche, course;
        public final List<List<Anim>> parFamille = new ArrayList<List<Anim>>();
        public final List<Anim> surPlace = new ArrayList<Anim>();     // tout sauf les déplacements : ce qu'il joue quand on le touche

        public static Biblio lire(InputStream flux) throws IOException {
            ByteArrayOutputStream tout = new ByteArrayOutputStream(1 << 21);
            byte[] bloc = new byte[1 << 16];
            int lus;
            while ((lus = flux.read(bloc)) > 0) tout.write(bloc, 0, lus);
            ByteBuffer b = ByteBuffer.wrap(tout.toByteArray()).order(ByteOrder.LITTLE_ENDIAN);
            if (b.remaining() < 8 || b.get() != 'S' || b.get() != 'T' || b.get() != 'K' || b.get() != '1') throw new IOException("animations.bin : format inconnu");
            Biblio r = new Biblio();
            for (int i = 0; i < FAMILLES.length; i++) r.parFamille.add(new ArrayList<Anim>());
            r.repos = new Anim[b.getInt()];
            for (int i = 0; i < r.repos.length; i++) r.repos[i] = anim(b);
            r.reception = anim(b);
            r.releve = anim(b);
            b.position(b.position() + 2 * N * 2);      // les deux poses de saut : pas de saut sur un fond d'écran
            String marche = texte(b), course = texte(b);
            r.toutes = new Anim[b.getInt()];
            for (int i = 0; i < r.toutes.length; i++) {
                Anim a = anim(b);
                r.toutes[i] = a;
                if (a.nom.equals(marche)) r.marche = a;
                if (a.nom.equals(course)) r.course = a;
                for (int f = 0; f < FAMILLES.length; f++)
                    if (FAMILLES[f].equals(a.famille)) {
                        r.parFamille.get(f).add(a);
                        if (!a.deplace) r.surPlace.add(a);
                    }
            }
            if (r.repos.length == 0 || r.toutes.length == 0) throw new IOException("animations.bin : vide");
            if (r.marche == null || r.course == null)
                for (Anim a : r.toutes) if (a.deplace) { if (r.marche == null) r.marche = a; if (r.course == null) r.course = a; }
            return r;
        }

        static String texte(ByteBuffer b) {
            byte[] octets = new byte[b.getShort() & 0xFFFF];
            b.get(octets);
            return new String(octets, StandardCharsets.UTF_8);
        }

        static Anim anim(ByteBuffer b) {
            Anim a = new Anim();
            a.nom = texte(b); a.anglais = texte(b); a.famille = texte(b);
            a.duree = Math.max(0.05f, b.getFloat());
            a.tours = b.getShort();
            int drapeaux = b.get();
            a.haut = (drapeaux & 1) != 0; a.deplace = (drapeaux & 2) != 0;
            a.vitesse = b.getFloat();
            a.objet = texte(b);
            a.nombre = b.getShort();
            a.poses = new short[a.nombre * N];
            b.asShortBuffer().get(a.poses);
            b.position(b.position() + a.poses.length * 2);
            return a;
        }
    }

    final class Bonhomme {
        float x;                                  // où ses pieds touchent le sol
        int face = 1, teinte;
        Anim anim, geste;
        float t, tours, tGeste, toursGeste, cible = Float.NaN, fondu = 1, prochaine, finBulle;
        boolean repos;
        String bulle;
        float[] pose = NEUTRE.clone(), depuis = NEUTRE.clone();
        final float[] calcul = new float[N], calculGeste = new float[N];

        Bonhomme(int numero) {
            teinte = numero % PALETTE.length;
            face = numero % 2 == 0 ? 1 : -1;
            x = marge() + hasard.nextFloat() * Math.max(1, largeur - 2 * marge());
            seReposer();
            prochaine = temps + 0.5f + hasard.nextFloat() * 3;      // ils ne démarrent pas tous ensemble
        }

        void jouer(Anim a, float combien) {
            if (a.haut) { geste = a; tGeste = 0; toursGeste = combien; return; }      // haut du corps : par-dessus le reste
            System.arraycopy(pose, 0, depuis, 0, N);
            fondu = 0;
            anim = a; t = 0; tours = combien; cible = Float.NaN; repos = false;
        }

        void seReposer() {
            jouer(biblio.repos[hasard.nextInt(biblio.repos.length)], Float.POSITIVE_INFINITY);
            repos = true;
            prochaine = temps + 2 + hasard.nextFloat() * 4;
        }

        void dire(String texte, float secondes) { bulle = texte; finBulle = temps + secondes; }

        // Avance le long du sol sans sortir de l'écran ; vrai s'il bute contre le bord.
        boolean avancer(float d) {
            float voulu = x + d, mini = marge(), maxi = Math.max(mini, largeur - marge());
            x = Math.max(mini, Math.min(maxi, voulu));
            return Math.abs(x - voulu) > 0.01f;
        }

        void allerVers(float la, Anim marche) {
            if (Math.abs(la - x) < 8 * taille) { seReposer(); return; }
            jouer(marche, Float.POSITIVE_INFINITY);
            cible = la;
            face = (la > x ? 1 : -1) * (marche.vitesse < 0 ? -1 : 1);
        }

        // Ce qu'il décide de faire quand il n'a rien en cours.
        void choisir() {
            int total = 0;
            for (int f = 0; f < FAMILLES.length; f++) if (!biblio.parFamille.get(f).isEmpty()) total += POIDS[f];
            if (total == 0) { prochaine = temps + 5; return; }
            int tirage = hasard.nextInt(total);
            for (int f = 0; f < FAMILLES.length; f++) {
                List<Anim> liste = biblio.parFamille.get(f);
                if (liste.isEmpty()) continue;
                tirage -= POIDS[f];
                if (tirage < 0) { demander(liste.get(hasard.nextInt(liste.size()))); return; }
            }
        }

        void demander(Anim a) {
            if (a.haut) {
                jouer(a, Math.max(1, a.tours));
                prochaine = temps + a.duree * Math.max(1, a.tours) + 2 + hasard.nextFloat() * 3;
            } else if (a.deplace) {
                float mini = marge() + 12 * taille, maxi = Math.max(mini, largeur - marge() - 12 * taille);
                float d = (80 * taille + hasard.nextFloat() * 0.6f * largeur) * (hasard.nextBoolean() ? 1 : -1), la = x + d;
                if (la < mini || la > maxi) la = x - d;
                allerVers(Math.max(mini, Math.min(maxi, la)), a);
            } else jouer(a, Math.max(1, a.tours));
        }

        void vivre(float dt) {
            t += dt / anim.duree;
            if (anim.deplace && !Float.isNaN(cible)) {
                float pas = Math.abs(anim.vitesse) * taille * dt, reste = cible - x;
                if (Math.abs(reste) <= pas || avancer(reste > 0 ? pas : -pas)) { seReposer(); return; }
            } else if (t >= tours) { seReposer(); return; }

            anim.pose(tours == 1 && !anim.deplace ? Math.min(1, t) : t - (float) Math.floor(t), calcul);
            if (geste != null) {
                tGeste += dt / geste.duree;
                if (tGeste >= toursGeste) { geste = null; System.arraycopy(pose, 0, depuis, 0, N); fondu = 0; }
                else {      // un geste du haut du corps posé sur ce que font les jambes
                    geste.pose(tGeste - (float) Math.floor(tGeste), calculGeste);
                    calcul[TORSE] += calculGeste[TORSE]; calcul[TETE] = calculGeste[TETE];
                    calcul[EP1] = calculGeste[EP1]; calcul[CO1] = calculGeste[CO1]; calcul[EP2] = calculGeste[EP2]; calcul[CO2] = calculGeste[CO2];
                }
            }
            adoucir(dt);
            if (repos && temps >= prochaine && geste == null) choisir();
        }

        // D'une animation à l'autre, la pose glisse au lieu de sauter.
        void adoucir(float dt) {
            if (fondu < 1) fondu += dt / 0.16f;
            if (fondu >= 1) { System.arraycopy(calcul, 0, pose, 0, N); return; }
            float k = fondu * fondu * (3 - 2 * fondu);
            for (int i = 0; i < N; i++) pose[i] = depuis[i] + (calcul[i] - depuis[i]) * k;
            float ecart = (calcul[ROT] - depuis[ROT]) % 360;       // la rotation prend le plus court chemin
            if (ecart > 180) ecart -= 360; else if (ecart < -180) ecart += 360;
            pose[ROT] = depuis[ROT] + ecart * k;
        }

        // Le doigt est-il sur lui ?
        boolean touche(float px, float py) {
            squelette(pose, os);
            float cx = x + face * (os[HANCHE] + os[CRANE]) / 2 * taille, cy = sol + (os[HANCHE + 1] + os[CRANE + 1]) / 2 * taille;
            return Math.hypot(px - cx, py - cy) < 62 * taille;
        }
    }

    final Biblio biblio;
    final Random hasard = new Random();
    final List<Bonhomme> bande = new ArrayList<Bonhomme>();
    final boolean francais;
    float largeur, hauteur, sol, taille = 1, temps;
    private final float[] os = new float[POINTS], ecran = new float[POINTS], ligne = new float[6];

    public Moteur(Biblio biblio, boolean francais) {
        this.biblio = biblio;
        this.francais = francais;
    }

    float marge() { return 28 * taille; }

    public int nombre() { return bande.size(); }

    // La taille de l'écran, la hauteur du sol (en pixels depuis le haut) et la taille des stickmen (1 = 110 pixels de haut environ).
    public void dimensionner(float largeur, float hauteur, float sol, float taille) {
        boolean premier = this.largeur <= 0;
        float avant = this.largeur;
        this.largeur = largeur; this.hauteur = hauteur; this.sol = sol; this.taille = taille;
        for (int i = 0; i < bande.size(); i++) {
            Bonhomme b = bande.get(i);
            if (premier) b.x = largeur * (i + 1f) / (bande.size() + 1);      // au départ, répartis sur toute la largeur
            else if (avant > 0 && avant != largeur) { b.x *= largeur / avant; b.cible = Float.isNaN(b.cible) ? b.cible : b.cible * largeur / avant; }
            b.avancer(0);
        }
    }

    public void peupler(int nombre) {
        nombre = Math.max(1, Math.min(6, nombre));
        while (bande.size() > nombre) bande.remove(bande.size() - 1);
        while (bande.size() < nombre) bande.add(new Bonhomme(bande.size()));
    }

    public void vivre(float dt) {
        dt = Math.max(0, Math.min(0.05f, dt));
        temps += dt;
        for (int i = 0; i < bande.size(); i++) bande.get(i).vivre(dt);
    }

    // Un doigt posé puis relevé : sur un stickman, il joue une animation au hasard et dit laquelle ; ailleurs, le
    // plus proche y va, en marchant ou en courant.
    public void toucher(float px, float py) {
        for (int i = bande.size() - 1; i >= 0; i--) {
            Bonhomme b = bande.get(i);
            if (!b.touche(px, py)) continue;
            if (biblio.surPlace.isEmpty()) return;
            Anim a = biblio.surPlace.get(hasard.nextInt(biblio.surPlace.size()));
            b.demander(a);
            b.dire(francais || a.anglais.length() == 0 ? a.nom : a.anglais, 2.2f);
            return;
        }
        Bonhomme proche = null;
        for (int i = 0; i < bande.size(); i++) if (proche == null || Math.abs(bande.get(i).x - px) < Math.abs(proche.x - px)) proche = bande.get(i);
        if (proche == null) return;
        float la = Math.max(marge(), Math.min(Math.max(marge(), largeur - marge()), px));
        proche.geste = null;
        proche.allerVers(la, Math.abs(la - proche.x) > 320 * taille ? biblio.course : biblio.marche);
    }

    // Positions des articulations, personnage tourné vers la droite, y vers le bas : le point le plus bas touche le sol (y = 0).
    static void squelette(float[] p, float[] o) {
        float rot = p[ROT], dos = rot + p[TORSE];
        o[HANCHE] = 0; o[HANCHE + 1] = 0;
        bout(o, HANCHE, COU, dos, L_TORSE, true);
        bout(o, COU, CRANE, dos + p[TETE], R_TETE + 1, true);
        float a = p[EP1] - rot;
        bout(o, COU, C1, a, L_BRAS, false); bout(o, C1, M1, a + p[CO1], L_BRAS, false);
        a = p[EP2] - rot;
        bout(o, COU, C2, a, L_BRAS, false); bout(o, C2, M2, a + p[CO2], L_BRAS, false);
        a = p[HA1] - rot;
        bout(o, HANCHE, G1, a, L_JAMBE, false); bout(o, G1, P1, a + p[GE1], L_JAMBE, false);
        a = p[HA2] - rot;
        bout(o, HANCHE, G2, a, L_JAMBE, false); bout(o, G2, P2, a + p[GE2], L_JAMBE, false);
        float plusBas = Math.max(o[CRANE + 1] + R_TETE, 0);
        for (int i = COU; i < POINTS; i += 2) if (i != CRANE) plusBas = Math.max(plusBas, o[i + 1]);
        float dx = p[IX], dy = -plusBas - p[AIR];
        for (int i = 0; i < POINTS; i += 2) { o[i] += dx; o[i + 1] += dy; }
    }

    // Un os : du point « de » au point « vers », d'une longueur donnée ; l'angle se compte depuis le bas (membres) ou depuis le haut (dos, tête).
    private static void bout(float[] o, int de, int vers, float degres, float longueur, boolean versLeHaut) {
        double r = Math.toRadians(degres);
        o[vers] = o[de] + (float) Math.sin(r) * longueur;
        o[vers + 1] = o[de + 1] + (float) Math.cos(r) * longueur * (versLeHaut ? -1 : 1);
    }

    public void dessiner(Toile toile) {
        for (int i = 0; i < bande.size(); i++) corps(bande.get(i), toile);
        for (int i = 0; i < bande.size(); i++) signes(bande.get(i), toile);      // ce qui s'écrit passe devant tout le monde
    }

    private float[] brise(int a, int b, int c) {
        ligne[0] = ecran[a]; ligne[1] = ecran[a + 1]; ligne[2] = ecran[b]; ligne[3] = ecran[b + 1];
        if (c >= 0) { ligne[4] = ecran[c]; ligne[5] = ecran[c + 1]; }
        return ligne;
    }

    private void placer(Bonhomme b) {
        squelette(b.pose, os);
        for (int i = 0; i < POINTS; i += 2) { ecran[i] = b.x + b.face * os[i] * taille; ecran[i + 1] = sol + os[i + 1] * taille; }
    }

    private void corps(Bonhomme b, Toile toile) {
        placer(b);
        int couleur = PALETTE[b.teinte][0];
        boolean creuse = PALETTE[b.teinte][1] == 1;
        toile.ovale(ecran[HANCHE], sol - 2 * taille, Math.max(8, 24 - b.pose[AIR] * 0.1f) * taille, 4 * taille, 0x47000000);      // son ombre
        for (int passe = 0; passe < 2; passe++) {            // d'abord le contour sombre de toute la silhouette, puis la couleur
            int encre = passe == 0 ? 0xFF0F1012 : couleur;
            float epaisseur = (passe == 0 ? 9.5f : 6f) * taille;
            toile.trait(brise(COU, C2, M2), 3, epaisseur, encre);
            toile.trait(brise(HANCHE, G2, P2), 3, epaisseur, encre);
            toile.trait(brise(HANCHE, COU, -1), 2, epaisseur, encre);
            if (passe == 1 && !creuse) toile.disque(ecran[CRANE], ecran[CRANE + 1], R_TETE * taille, couleur);
            toile.anneau(ecran[CRANE], ecran[CRANE + 1], R_TETE * taille, epaisseur, encre);
            toile.trait(brise(COU, C1, M1), 3, epaisseur, encre);
            toile.trait(brise(HANCHE, G1, P1), 3, epaisseur, encre);
        }
        String objet = b.geste != null && b.geste.objet.length() > 0 ? b.geste.objet : b.anim.objet;
        float longueur = objet.equals("baton") ? 54 : objet.equals("epee") ? 42 : objet.equals("club") || objet.equals("raquette") ? 30 : 0;
        if (longueur > 0) {                                  // tenu dans la main, dans le prolongement de l'avant-bras
            float dx = ecran[M1] - ecran[C1], dy = ecran[M1 + 1] - ecran[C1 + 1], l = Math.max(1, (float) Math.hypot(dx, dy));
            ligne[0] = ecran[M1]; ligne[1] = ecran[M1 + 1];
            ligne[2] = ecran[M1] + dx / l * longueur * taille; ligne[3] = ecran[M1 + 1] + dy / l * longueur * taille;
            toile.trait(ligne, 2, 3.5f * taille, 0xFFE8E8EE);
        }
    }

    // Les petits signes au-dessus de la tête (note, cœur, point d'exclamation…) et sa bulle.
    private void signes(Bonhomme b, Toile toile) {
        placer(b);
        String objet = b.geste != null && b.geste.objet.length() > 0 ? b.geste.objet : b.anim.objet;
        String signe = objet.equals("note") ? "♪" : objet.equals("zzz") ? "z Z" : objet.equals("coeur") ? "♥" : objet.equals("exclam") ? "!" : objet.equals("question") ? "?" : null;
        if (signe != null)
            toile.texte(signe, ecran[CRANE] + 16 * taille, ecran[CRANE + 1] - (30 + 5 * (float) Math.sin(temps * 4)) * taille, 18 * taille, true, objet.equals("coeur") ? 0xFFFF5A8A : 0xFFF4F4F8);
        if (b.bulle != null && temps < b.finBulle) {
            float l = toile.largeurTexte(b.bulle, 13 * taille, false) + 18 * taille, h = 24 * taille;
            float cx = Math.max(l / 2 + 4 * taille, Math.min(largeur - l / 2 - 4 * taille, ecran[CRANE])), cy = ecran[CRANE + 1] - 42 * taille;
            toile.pastille(cx, cy, l, h, 0xFFFAF9F5);
            toile.texte(b.bulle, cx, cy, 13 * taille, false, 0xFF303030);
        }
    }
}
