// Le fond d'écran animé : les stickmen vivent sur l'écran d'accueil du téléphone, derrière les icônes.
// Android appelle ce service quand on le choisit comme fond d'écran. Tout le comportement est dans Moteur ;
// ici on ne fait que lui prêter un Canvas, lui donner l'heure et lui transmettre les doigts.
// Il ne dessine que lorsque l'écran d'accueil se voit (30 images par seconde), et ne demande aucune permission.
package io.github.minecraft2048.stickman;

import android.content.Context;
import android.content.SharedPreferences;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Canvas;
import android.graphics.LinearGradient;
import android.graphics.Matrix;
import android.graphics.Paint;
import android.graphics.Path;
import android.graphics.RectF;
import android.graphics.Shader;
import android.graphics.Typeface;
import android.os.Build;
import android.os.Handler;
import android.os.Looper;
import android.os.SystemClock;
import android.service.wallpaper.WallpaperService;
import android.view.MotionEvent;
import android.view.SurfaceHolder;
import java.io.File;
import java.io.IOException;
import java.io.InputStream;
import java.util.Locale;

public class FondEcran extends WallpaperService {
    // les réglages, partagés avec la page Reglages
    static final String REGLAGES = "reglages", NOMBRE = "nombre", SOL = "sol", TAILLE = "taille", FOND = "fond", IMAGE = "image";
    static final String FICHIER_IMAGE = "fond.jpg";
    static final int NOMBRE_DEFAUT = 3, SOL_DEFAUT = 12, TAILLE_DEFAUT = 100;

    // L'aperçu et le vrai fond d'écran tournent en même temps : les animations ne sont lues qu'une fois.
    private static Moteur.Biblio commune;

    static synchronized Moteur.Biblio animations(Context contexte) throws IOException {
        if (commune == null) {
            InputStream flux = contexte.getAssets().open("animations.bin");
            try { commune = Moteur.Biblio.lire(flux); } finally { flux.close(); }
        }
        return commune;
    }

    @Override
    public Engine onCreateEngine() { return new Machine(); }

    final class Machine extends Engine implements Moteur.Toile, SharedPreferences.OnSharedPreferenceChangeListener {
        private final Handler fil = new Handler(Looper.getMainLooper());
        private final Paint plume = new Paint(Paint.ANTI_ALIAS_FLAG), plein = new Paint(Paint.ANTI_ALIAS_FLAG), lettres = new Paint(Paint.ANTI_ALIAS_FLAG),
            photo = new Paint(Paint.FILTER_BITMAP_FLAG);
        private final Path chemin = new Path();
        private final RectF cadre = new RectF();
        private final Matrix matrice = new Matrix();
        private SharedPreferences reglages;
        private Moteur.Biblio biblio;
        private Moteur moteur;
        private Canvas toile;
        private Bitmap fond;
        private long dateFond, avant, debutDoigt;
        private Shader degrade;
        private int largeur, hauteur;
        private float doigtX, doigtY;
        private boolean visible, detruit;
        private boolean materiel = Build.VERSION.SDK_INT >= 26;      // dessin par la carte graphique quand le téléphone le permet

        private final Runnable imageSuivante = new Runnable() {
            @Override
            public void run() { peindre(); }
        };

        @Override
        public void onCreate(SurfaceHolder surface) {
            super.onCreate(surface);
            setTouchEventsEnabled(true);
            plume.setStyle(Paint.Style.STROKE);
            plume.setStrokeCap(Paint.Cap.ROUND);
            plume.setStrokeJoin(Paint.Join.ROUND);
            lettres.setTextAlign(Paint.Align.CENTER);
            reglages = getSharedPreferences(REGLAGES, MODE_PRIVATE);
            reglages.registerOnSharedPreferenceChangeListener(this);
            new Thread(new Runnable() {                          // la lecture des animations ne bloque pas l'affichage
                @Override
                public void run() {
                    try {
                        final Moteur.Biblio lue = animations(FondEcran.this);
                        fil.post(new Runnable() {
                            @Override
                            public void run() { if (!detruit) { biblio = lue; installer(); } }
                        });
                    } catch (Exception e) { }                    // sans animations, il reste le fond, sans stickmen
                }
            }, "animations").start();
        }

        @Override
        public void onDestroy() {
            detruit = true; visible = false;
            fil.removeCallbacks(imageSuivante);
            reglages.unregisterOnSharedPreferenceChangeListener(this);
            super.onDestroy();
        }

        @Override
        public void onSurfaceChanged(SurfaceHolder surface, int format, int l, int h) {
            super.onSurfaceChanged(surface, format, l, h);
            largeur = l; hauteur = h;
            degrade = new LinearGradient(0, 0, 0, h, 0xFF222D4A, 0xFF0D0F15, Shader.TileMode.CLAMP);
            installer();
            peindre();
        }

        @Override
        public void onSurfaceDestroyed(SurfaceHolder surface) {
            visible = false;
            fil.removeCallbacks(imageSuivante);
            super.onSurfaceDestroyed(surface);
        }

        @Override
        public void onVisibilityChanged(boolean v) {
            visible = v;
            if (v) { avant = 0; appliquer(); peindre(); }        // de retour sur l'écran d'accueil : on reprend là où ils en étaient
            else fil.removeCallbacks(imageSuivante);
        }

        @Override
        public void onSharedPreferenceChanged(SharedPreferences r, String cle) { appliquer(); }

        @Override
        public void onTouchEvent(MotionEvent e) {
            // un doigt posé puis relevé au même endroit ; les glissements (changer de page) ne les dérangent pas
            if (e.getActionMasked() == MotionEvent.ACTION_DOWN) { doigtX = e.getX(); doigtY = e.getY(); debutDoigt = e.getEventTime(); }
            else if (e.getActionMasked() == MotionEvent.ACTION_UP && moteur != null && e.getEventTime() - debutDoigt < 350
                && Math.hypot(e.getX() - doigtX, e.getY() - doigtY) < 20 * getResources().getDisplayMetrics().density)
                moteur.toucher(e.getX(), e.getY());
        }

        private void installer() {
            if (biblio == null || largeur <= 0 || hauteur <= 0) return;
            if (moteur == null) moteur = new Moteur(biblio, "fr".equals(Locale.getDefault().getLanguage()));
            appliquer();
        }

        private void appliquer() {
            if (largeur <= 0 || hauteur <= 0) return;
            if (moteur != null) {
                moteur.peupler(reglages.getInt(NOMBRE, NOMBRE_DEFAUT));
                float sol = hauteur * (1 - reglages.getInt(SOL, SOL_DEFAUT) / 100f);
                moteur.dimensionner(largeur, hauteur, sol, Math.min(largeur, hauteur) / 420f * reglages.getInt(TAILLE, TAILLE_DEFAUT) / 100f);
            }
            File fichier = new File(getFilesDir(), FICHIER_IMAGE);
            if (!"image".equals(reglages.getString(FOND, "sombre")) || !fichier.exists()) fond = null;
            else if (fond == null || dateFond != fichier.lastModified()) {
                try {
                    BitmapFactory.Options o = new BitmapFactory.Options();
                    o.inJustDecodeBounds = true;
                    BitmapFactory.decodeFile(fichier.getPath(), o);
                    int k = 1;
                    while (Math.max(o.outWidth, o.outHeight) / (k * 2) >= Math.max(largeur, hauteur)) k *= 2;      // pas plus grande que l'écran
                    o = new BitmapFactory.Options();
                    o.inSampleSize = k;
                    fond = BitmapFactory.decodeFile(fichier.getPath(), o);
                    dateFond = fichier.lastModified();
                } catch (RuntimeException e) { fond = null; } catch (OutOfMemoryError e) { fond = null; }
            }
        }

        private void peindre() {
            fil.removeCallbacks(imageSuivante);
            if (!visible || detruit || largeur <= 0) return;
            long maintenant = SystemClock.elapsedRealtime();
            float dt = avant == 0 ? 0 : (maintenant - avant) / 1000f;
            avant = maintenant;
            SurfaceHolder surface = getSurfaceHolder();
            Canvas c = null;
            try {
                c = materiel ? surface.lockHardwareCanvas() : surface.lockCanvas();
                if (c != null) {
                    if (fond != null) {                          // l'image choisie, agrandie pour couvrir l'écran
                        float k = Math.max(largeur / (float) fond.getWidth(), hauteur / (float) fond.getHeight());
                        matrice.setScale(k, k);
                        matrice.postTranslate((largeur - fond.getWidth() * k) / 2, (hauteur - fond.getHeight() * k) / 2);
                        c.drawBitmap(fond, matrice, photo);
                    } else {
                        plein.setShader(degrade);
                        c.drawRect(0, 0, largeur, hauteur, plein);
                        plein.setShader(null);
                    }
                    if (moteur != null) {
                        moteur.vivre(dt);
                        toile = c;
                        moteur.dessiner(this);
                        toile = null;
                    }
                }
            } catch (RuntimeException e) {
                materiel = false;                                // pas de dessin matériel sur ce téléphone : dessin ordinaire désormais
            } finally {
                if (c != null) try { surface.unlockCanvasAndPost(c); } catch (RuntimeException e) { }
            }
            if (visible) fil.postDelayed(imageSuivante, 33);
        }

        // ---------------------------------------------------------------- ce que le moteur demande de dessiner

        @Override
        public void trait(float[] points, int nombre, float epaisseur, int couleur) {
            chemin.rewind();
            chemin.moveTo(points[0], points[1]);
            for (int i = 1; i < nombre; i++) chemin.lineTo(points[2 * i], points[2 * i + 1]);
            plume.setColor(couleur);
            plume.setStrokeWidth(epaisseur);
            toile.drawPath(chemin, plume);
        }

        @Override
        public void disque(float x, float y, float rayon, int couleur) {
            plein.setColor(couleur);
            toile.drawCircle(x, y, rayon, plein);
        }

        @Override
        public void anneau(float x, float y, float rayon, float epaisseur, int couleur) {
            plume.setColor(couleur);
            plume.setStrokeWidth(epaisseur);
            toile.drawCircle(x, y, rayon, plume);
        }

        @Override
        public void ovale(float x, float y, float rayonX, float rayonY, int couleur) {
            plein.setColor(couleur);
            cadre.set(x - rayonX, y - rayonY, x + rayonX, y + rayonY);
            toile.drawOval(cadre, plein);
        }

        @Override
        public void pastille(float x, float y, float l, float h, int couleur) {
            plein.setColor(couleur);
            cadre.set(x - l / 2, y - h / 2, x + l / 2, y + h / 2);
            toile.drawRoundRect(cadre, h / 2, h / 2, plein);
        }

        private void police(float taille, boolean gras) {
            lettres.setTextSize(taille);
            lettres.setTypeface(gras ? Typeface.DEFAULT_BOLD : Typeface.DEFAULT);
        }

        @Override
        public float largeurTexte(String texte, float taille, boolean gras) {
            police(taille, gras);
            return lettres.measureText(texte);
        }

        @Override
        public void texte(String texte, float x, float y, float taille, boolean gras, int couleur) {
            police(taille, gras);
            lettres.setColor(couleur);
            toile.drawText(texte, x, y - (lettres.ascent() + lettres.descent()) / 2, lettres);
        }
    }
}
