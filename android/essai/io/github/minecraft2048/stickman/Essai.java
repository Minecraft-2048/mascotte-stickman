// Essai du moteur du fond d'écran sur un PC, sans téléphone : il charge animations.bin, fait vivre quatre
// stickmen sur un écran de téléphone imaginaire (1080 x 2340), les touche du doigt, et écrit une planche de
// huit vues en PNG. Puis il les fait vivre dix minutes d'affilée pour vérifier que rien ne déraille.
//   java -cp <classes> io.github.minecraft2048.stickman.Essai assets/animations.bin planche.png
// Ce fichier ne fait pas partie de l'appli (voir construire.ps1).
package io.github.minecraft2048.stickman;

import java.awt.BasicStroke;
import java.awt.Color;
import java.awt.Font;
import java.awt.GradientPaint;
import java.awt.Graphics2D;
import java.awt.RenderingHints;
import java.awt.geom.Ellipse2D;
import java.awt.geom.Path2D;
import java.awt.geom.RoundRectangle2D;
import java.awt.image.BufferedImage;
import java.io.File;
import java.io.FileInputStream;
import java.io.InputStream;
import javax.imageio.ImageIO;

public class Essai implements Moteur.Toile {
    Graphics2D g;

    public static void main(String[] args) throws Exception {
        final int largeur = 1080, hauteur = 2340, vues = 8, haut = 1500;      // on ne montre que le bas de l'écran
        final float reduc = 1 / 3f;
        Moteur.Biblio biblio;
        try (InputStream flux = new FileInputStream(args[0])) { biblio = Moteur.Biblio.lire(flux); }
        System.out.println(biblio.toutes.length + " animations, " + biblio.repos.length + " attentes, marche = " + biblio.marche.nom + ", course = " + biblio.course.nom
            + ", sur place = " + biblio.surPlace.size());
        Moteur moteur = new Moteur(biblio, true);
        moteur.peupler(4);
        float sol = hauteur * 0.88f;
        moteur.dimensionner(largeur, hauteur, sol, largeur / 420f);

        int l = Math.round(largeur * reduc), h = Math.round((hauteur - haut) * reduc);
        BufferedImage planche = new BufferedImage(l * vues, h, BufferedImage.TYPE_INT_RGB);
        Essai toile = new Essai();
        for (int vue = 0; vue < vues; vue++) {
            for (int i = 0; i < 80; i++) moteur.vivre(1 / 30f);
            if (vue == 3) {                                  // un doigt sur le premier : une animation et sa bulle
                Moteur.Bonhomme b = moteur.bande.get(0);
                moteur.toucher(b.x, sol - 60 * moteur.taille);
                System.out.println("touché : « " + b.bulle + " » (" + b.anim.nom + ")");
                for (int i = 0; i < 12; i++) moteur.vivre(1 / 30f);
            }
            if (vue == 5) {                                  // un doigt loin de tout le monde : le plus proche y court
                moteur.toucher(largeur - 40, sol - 600);
                for (int i = 0; i < 20; i++) moteur.vivre(1 / 30f);
            }
            Graphics2D g = planche.createGraphics();
            g.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON);
            g.setRenderingHint(RenderingHints.KEY_TEXT_ANTIALIASING, RenderingHints.VALUE_TEXT_ANTIALIAS_ON);
            g.setClip(vue * l, 0, l, h);
            g.translate(vue * l, 0);
            g.scale(reduc, reduc);
            g.translate(0, -haut);
            g.setPaint(new GradientPaint(0, haut, new Color(0x2A3550), 0, hauteur, new Color(0x101218)));
            g.fillRect(0, haut, largeur, hauteur - haut);
            g.setColor(new Color(255, 255, 255, 40));        // une barre d'icônes, pour voir où ils se tiennent
            g.fill(new RoundRectangle2D.Float(60, sol + 10, largeur - 120, hauteur - sol - 60, 80, 80));
            for (int k = 0; k < 4; k++) { g.setColor(new Color[] { new Color(0x4FC3F7), new Color(0xFFB74D), new Color(0x81C784), new Color(0xE57373) }[k]); g.fill(new RoundRectangle2D.Float(130 + k * 225, sol + 50, 150, 150, 40, 40)); }
            toile.g = g;
            moteur.dessiner(toile);
            g.dispose();
        }
        ImageIO.write(planche, "png", new File(args[1]));

        // en option, une vue de l'écran entier avec de fausses icônes : l'aperçu du README (une simulation, pas une capture)
        if (args.length > 2) {
            BufferedImage ecran = new BufferedImage(largeur / 2, hauteur / 2, BufferedImage.TYPE_INT_RGB);
            Graphics2D g = ecran.createGraphics();
            g.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON);
            g.setRenderingHint(RenderingHints.KEY_TEXT_ANTIALIASING, RenderingHints.VALUE_TEXT_ANTIALIAS_ON);
            g.scale(0.5, 0.5);
            g.setPaint(new GradientPaint(0, 0, new Color(0x3A4E86), 0, hauteur, new Color(0x121522)));
            g.fillRect(0, 0, largeur, hauteur);
            Color[] teintes = { new Color(0x4FC3F7), new Color(0xFFB74D), new Color(0x81C784), new Color(0xE57373), new Color(0xBA68C8), new Color(0xFFF176), new Color(0x4DB6AC), new Color(0xF06292) };
            for (int r = 0; r < 4; r++)
                for (int k = 0; k < 4; k++) {
                    g.setColor(teintes[(r * 3 + k) % teintes.length]);
                    g.fill(new RoundRectangle2D.Float(105 + k * 240, 260 + r * 300, 150, 150, 44, 44));
                    g.setColor(new Color(255, 255, 255, 120));
                    g.fill(new RoundRectangle2D.Float(125 + k * 240, 430 + r * 300, 110, 16, 16, 16));
                }
            g.setColor(new Color(255, 255, 255, 40));
            g.fill(new RoundRectangle2D.Float(60, sol + 10, largeur - 120, hauteur - sol - 60, 80, 80));
            for (int k = 0; k < 4; k++) { g.setColor(teintes[(k + 4) % teintes.length]); g.fill(new RoundRectangle2D.Float(105 + k * 240, sol + 50, 150, 150, 44, 44)); }
            moteur.toucher(moteur.bande.get(1).x, sol - 60 * moteur.taille);
            for (int i = 0; i < 14; i++) moteur.vivre(1 / 30f);
            toile.g = g;
            moteur.dessiner(toile);
            g.dispose();
            ImageIO.write(ecran, "png", new File(args[2]));
        }

        // dix minutes de vie : rien ne doit sortir de l'écran ni devenir un nombre impossible
        int faits = 0;
        float mini = Float.MAX_VALUE, maxi = -Float.MAX_VALUE;
        java.util.Set<String> vus = new java.util.HashSet<String>();
        moteur.peupler(6);
        for (int i = 0; i < 30 * 600; i++) {
            moteur.vivre(1 / 30f);
            if (i % 200 == 0) moteur.toucher((float) Math.random() * largeur, sol - (float) Math.random() * 400);
            for (Moteur.Bonhomme b : moteur.bande) {
                mini = Math.min(mini, b.x); maxi = Math.max(maxi, b.x);
                vus.add(b.anim.nom);
                for (float v : b.pose) if (Float.isNaN(v) || Float.isInfinite(v)) throw new IllegalStateException("pose impossible pendant « " + b.anim.nom + " »");
            }
            if (i % 30 == 0) { moteur.dessiner(toile); faits++; }
        }
        System.out.println("dix minutes : x entre " + Math.round(mini) + " et " + Math.round(maxi) + " (écran : 0 à " + largeur + "), " + vus.size() + " animations différentes jouées, " + faits + " images dessinées");
    }

    static Color couleur(int argb) { return new Color(argb, true); }

    public void trait(float[] points, int nombre, float epaisseur, int couleur) {
        Path2D.Float chemin = new Path2D.Float();
        chemin.moveTo(points[0], points[1]);
        for (int i = 1; i < nombre; i++) chemin.lineTo(points[2 * i], points[2 * i + 1]);
        g.setColor(couleur(couleur));
        g.setStroke(new BasicStroke(epaisseur, BasicStroke.CAP_ROUND, BasicStroke.JOIN_ROUND));
        g.draw(chemin);
    }

    public void disque(float x, float y, float rayon, int couleur) {
        g.setColor(couleur(couleur));
        g.fill(new Ellipse2D.Float(x - rayon, y - rayon, 2 * rayon, 2 * rayon));
    }

    public void anneau(float x, float y, float rayon, float epaisseur, int couleur) {
        g.setColor(couleur(couleur));
        g.setStroke(new BasicStroke(epaisseur));
        g.draw(new Ellipse2D.Float(x - rayon, y - rayon, 2 * rayon, 2 * rayon));
    }

    public void ovale(float x, float y, float rayonX, float rayonY, int couleur) {
        g.setColor(couleur(couleur));
        g.fill(new Ellipse2D.Float(x - rayonX, y - rayonY, 2 * rayonX, 2 * rayonY));
    }

    public void pastille(float x, float y, float largeur, float hauteur, int couleur) {
        g.setColor(couleur(couleur));
        g.fill(new RoundRectangle2D.Float(x - largeur / 2, y - hauteur / 2, largeur, hauteur, hauteur, hauteur));
    }

    Font police(float taille, boolean gras) { return new Font("SansSerif", gras ? Font.BOLD : Font.PLAIN, 1).deriveFont(taille); }

    public float largeurTexte(String texte, float taille, boolean gras) { return g.getFontMetrics(police(taille, gras)).stringWidth(texte); }

    public void texte(String texte, float x, float y, float taille, boolean gras, int couleur) {
        g.setFont(police(taille, gras));
        g.setColor(couleur(couleur));
        java.awt.FontMetrics m = g.getFontMetrics();
        g.drawString(texte, x - m.stringWidth(texte) / 2f, y + (m.getAscent() - m.getDescent()) / 2f);
    }
}
