// La page de l'appli : un bouton pour poser les stickmen en fond d'écran, et leurs réglages (combien, à quelle
// hauteur ils marchent, leur taille, l'image derrière eux). Un fond d'écran animé remplace le fond d'écran :
// pour garder le sien, on choisit ici la même image, qui est recopiée dans le dossier privé de l'appli.
package io.github.minecraft2048.stickman;

import android.app.Activity;
import android.app.WallpaperManager;
import android.content.ActivityNotFoundException;
import android.content.ComponentName;
import android.content.Intent;
import android.content.SharedPreferences;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Matrix;
import android.media.ExifInterface;
import android.net.Uri;
import android.os.Bundle;
import android.util.DisplayMetrics;
import android.util.TypedValue;
import android.view.View;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.SeekBar;
import android.widget.TextView;
import android.widget.Toast;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;

public class Reglages extends Activity {
    private static final int CHOISIR = 1;
    private SharedPreferences reglages;
    private LinearLayout page;

    @Override
    protected void onCreate(Bundle etat) {
        super.onCreate(etat);
        reglages = getSharedPreferences(FondEcran.REGLAGES, MODE_PRIVATE);
        int marge = Math.round(20 * getResources().getDisplayMetrics().density);
        page = new LinearLayout(this);
        page.setOrientation(LinearLayout.VERTICAL);
        page.setPadding(marge, marge, marge, marge);

        texte(R.string.nom, 26, 0);
        texte(R.string.explication, 15, marge / 2);
        bouton(R.string.poser, marge, new View.OnClickListener() {
            @Override
            public void onClick(View v) { poser(); }
        });
        curseur(R.string.nombre, FondEcran.NOMBRE, 1, 6, FondEcran.NOMBRE_DEFAUT, marge);
        curseur(R.string.sol, FondEcran.SOL, 0, 40, FondEcran.SOL_DEFAUT, marge);
        texte(R.string.aide_sol, 13, 0);
        curseur(R.string.taille, FondEcran.TAILLE, 60, 160, FondEcran.TAILLE_DEFAUT, marge);
        bouton(R.string.image, marge, new View.OnClickListener() {
            @Override
            public void onClick(View v) { choisir(); }
        });
        texte(R.string.aide_image, 13, 0);
        bouton(R.string.sombre, marge / 2, new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                reglages.edit().putString(FondEcran.FOND, "sombre").apply();
                Toast.makeText(Reglages.this, R.string.sombre_fait, Toast.LENGTH_SHORT).show();
            }
        });
        texte(R.string.astuce, 13, marge);

        ScrollView defile = new ScrollView(this);
        defile.addView(page);
        setContentView(defile);
    }

    private void texte(int quoi, int taille, int dessus) {
        TextView t = new TextView(this);
        t.setText(quoi);
        t.setTextSize(TypedValue.COMPLEX_UNIT_SP, taille);
        t.setPadding(0, dessus, 0, 0);
        page.addView(t);
    }

    private void bouton(int quoi, int dessus, View.OnClickListener action) {
        Button b = new Button(this);
        b.setText(quoi);
        b.setAllCaps(false);
        b.setOnClickListener(action);
        LinearLayout.LayoutParams place = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MATCH_PARENT, LinearLayout.LayoutParams.WRAP_CONTENT);
        place.topMargin = dessus;
        page.addView(b, place);
    }

    // Un réglage entier : son libellé affiche la valeur, et le fond d'écran la prend dès qu'on bouge le curseur.
    private void curseur(final int libelle, final String cle, final int mini, int maxi, int defaut, int dessus) {
        final TextView t = new TextView(this);
        int valeur = Math.max(mini, Math.min(maxi, reglages.getInt(cle, defaut)));
        t.setText(getString(libelle, valeur));
        t.setTextSize(TypedValue.COMPLEX_UNIT_SP, 16);
        t.setPadding(0, dessus, 0, 0);
        SeekBar barre = new SeekBar(this);
        barre.setMax(maxi - mini);
        barre.setProgress(valeur - mini);
        barre.setOnSeekBarChangeListener(new SeekBar.OnSeekBarChangeListener() {
            @Override
            public void onProgressChanged(SeekBar b, int position, boolean parLeDoigt) {
                t.setText(getString(libelle, mini + position));
                if (parLeDoigt) reglages.edit().putInt(cle, mini + position).apply();
            }

            @Override
            public void onStartTrackingTouch(SeekBar b) { }

            @Override
            public void onStopTrackingTouch(SeekBar b) { }
        });
        page.addView(t);
        page.addView(barre);
    }

    // Ouvre l'aperçu d'Android pour ce fond d'écran (ou, à défaut, la liste des fonds d'écran animés).
    private void poser() {
        Intent demande = new Intent(WallpaperManager.ACTION_CHANGE_LIVE_WALLPAPER);
        demande.putExtra(WallpaperManager.EXTRA_LIVE_WALLPAPER_COMPONENT, new ComponentName(this, FondEcran.class));
        try { startActivity(demande); }
        catch (ActivityNotFoundException e) {
            try { startActivity(new Intent(WallpaperManager.ACTION_LIVE_WALLPAPER_CHOOSER)); }
            catch (ActivityNotFoundException e2) { Toast.makeText(this, R.string.pas_de_fond, Toast.LENGTH_LONG).show(); }
        }
    }

    private void choisir() {
        Intent demande = new Intent(Intent.ACTION_OPEN_DOCUMENT);
        demande.addCategory(Intent.CATEGORY_OPENABLE);
        demande.setType("image/*");
        try { startActivityForResult(demande, CHOISIR); }
        catch (ActivityNotFoundException e) { Toast.makeText(this, R.string.image_ratee, Toast.LENGTH_LONG).show(); }
    }

    @Override
    protected void onActivityResult(int demande, int resultat, Intent donnees) {
        super.onActivityResult(demande, resultat, donnees);
        if (demande == CHOISIR && resultat == RESULT_OK && donnees != null && donnees.getData() != null) garder(donnees.getData());
    }

    // Recopie l'image choisie dans le dossier de l'appli, ramenée à la taille de l'écran et remise à l'endroit.
    private void garder(final Uri image) {
        new Thread(new Runnable() {
            @Override
            public void run() {
                boolean faite = false;
                try {
                    BitmapFactory.Options o = new BitmapFactory.Options();
                    o.inJustDecodeBounds = true;
                    InputStream flux = getContentResolver().openInputStream(image);
                    try { BitmapFactory.decodeStream(flux, null, o); } finally { flux.close(); }
                    DisplayMetrics ecran = getResources().getDisplayMetrics();
                    int voulu = Math.max(ecran.widthPixels, ecran.heightPixels), k = 1;
                    while (Math.max(o.outWidth, o.outHeight) / (k * 2) >= voulu) k *= 2;
                    o = new BitmapFactory.Options();
                    o.inSampleSize = k;
                    Bitmap b;
                    flux = getContentResolver().openInputStream(image);
                    try { b = BitmapFactory.decodeStream(flux, null, o); } finally { flux.close(); }
                    int tourne = 0;                                // une photo peut être rangée couchée, avec la consigne de la tourner
                    flux = getContentResolver().openInputStream(image);
                    try {
                        int sens = new ExifInterface(flux).getAttributeInt(ExifInterface.TAG_ORIENTATION, ExifInterface.ORIENTATION_NORMAL);
                        tourne = sens == ExifInterface.ORIENTATION_ROTATE_90 ? 90 : sens == ExifInterface.ORIENTATION_ROTATE_180 ? 180 : sens == ExifInterface.ORIENTATION_ROTATE_270 ? 270 : 0;
                    } catch (Exception e) { } finally { flux.close(); }
                    if (b != null && tourne != 0) {
                        Matrix m = new Matrix();
                        m.postRotate(tourne);
                        b = Bitmap.createBitmap(b, 0, 0, b.getWidth(), b.getHeight(), m, true);
                    }
                    if (b != null && Math.max(b.getWidth(), b.getHeight()) > voulu) {      // jamais plus grande que l'écran : la mémoire du téléphone est comptée
                        float reduit = voulu / (float) Math.max(b.getWidth(), b.getHeight());
                        b = Bitmap.createScaledBitmap(b, Math.max(1, Math.round(b.getWidth() * reduit)), Math.max(1, Math.round(b.getHeight() * reduit)), true);
                    }
                    if (b != null) {
                        File provisoire = new File(getFilesDir(), "fond.tmp"), fichier = new File(getFilesDir(), FondEcran.FICHIER_IMAGE);
                        FileOutputStream sortie = new FileOutputStream(provisoire);
                        try { b.compress(Bitmap.CompressFormat.JPEG, 92, sortie); } finally { sortie.close(); }
                        fichier.delete();
                        faite = provisoire.renameTo(fichier);
                    }
                } catch (Exception e) { } catch (OutOfMemoryError e) { }
                final boolean ok = faite;
                runOnUiThread(new Runnable() {
                    @Override
                    public void run() {
                        if (ok) reglages.edit().putString(FondEcran.FOND, "image").putLong(FondEcran.IMAGE, System.currentTimeMillis()).apply();
                        Toast.makeText(Reglages.this, ok ? R.string.image_faite : R.string.image_ratee, Toast.LENGTH_LONG).show();
                    }
                });
            }
        }, "image").start();
    }
}
