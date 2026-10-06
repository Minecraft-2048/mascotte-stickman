// Langue de l'interface. Le programme est écrit en français ; quand Windows n'est pas en français (ou que
// le réglage le demande), chaque texte affiché passe par Langue.T, qui le cherche dans le dictionnaire
// ci-dessous. Un texte absent du dictionnaire reste en français : rien ne casse, il manque juste une ligne.
// Les noms internes (ceux de --jouer et des réglages) restent en français dans les deux langues.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MascotteStickman
{
    static class Langue
    {
        static Dictionary<string, string> dico;
        static readonly Regex compte = new Regex(@"^(.*?)  \((\d+)\)$");

        public static bool Force;                               // --anglais sur la ligne de commande (planches du README)

        public static bool Anglais
        {
            get
            {
                if (Force) return true;
                int reglage = (int)R.D("langue");                // 0 : comme Windows, 1 : français, 2 : anglais
                return reglage == 2 || (reglage == 0 && CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "fr");
            }
        }

        public static string T(string fr) { return Anglais ? EnAnglais(fr) : fr; }

        public static string EnAnglais(string fr)
        {
            if (string.IsNullOrEmpty(fr)) return fr;
            if (dico == null)
            {
                dico = new Dictionary<string, string>();
                for (int i = 0; i + 1 < paires.Length; i += 2) dico[paires[i]] = paires[i + 1];
            }
            string en;
            if (dico.TryGetValue(fr, out en)) return en;
            // textes composés : « bras + jambes » des danses, variantes de l'autre main, familles suivies de leur compte
            int plus = fr.IndexOf(" + ", StringComparison.Ordinal);
            if (plus > 0) return EnAnglais(fr.Substring(0, plus)) + " + " + EnAnglais(fr.Substring(plus + 3));
            foreach (string[] suffixe in suffixes)
                if (fr.EndsWith(suffixe[0], StringComparison.Ordinal)) return EnAnglais(fr.Substring(0, fr.Length - suffixe[0].Length)) + suffixe[1];
            Match m = compte.Match(fr);
            if (m.Success) return EnAnglais(m.Groups[1].Value) + "  (" + m.Groups[2].Value + ")";
            int tiret = fr.IndexOf("  —  ", StringComparison.Ordinal);
            if (tiret > 0) return EnAnglais(fr.Substring(0, tiret)) + "  —  " + EnAnglais(fr.Substring(tiret + 5));
            return fr;
        }

        static readonly string[][] suffixes =
        {
            new[] { " (autre main)", " (other hand)" }, new[] { " (autre jambe)", " (other leg)" },
            new[] { "  — tête creuse", "  — ring head" }, new[] { " (tête creuse)", " (ring head)" }, new[] { " (tête pleine)", " (solid head)" },
        };

        static readonly string[] paires =
        {
            // ---------------------------------------------------------------- menus et fenêtre des paramètres
            "Paramètres…", "Settings…",
            "animations", "animations",
            "réglages", "settings",
            "Couleur", "Colour",
            "Arc-en-ciel", "Rainbow",
            "Jouer une animation", "Play an animation",
            "Amis", "Friends",
            "Ajouter un stickman", "Add a stickman",
            "Renvoyer celui-ci", "Send this one away",
            "Aller voir un ami", "Go and see a friend",
            "Danser sur la musique", "Dance to the music",
            "Sauter sur une fenêtre", "Jump onto a window",
            "Se téléporter", "Teleport",
            "Mode farceur : il ferme des fenêtres", "Prank mode: he closes windows",
            "Mode muet (plus aucun bruit)", "Mute (no sound at all)",
            "Correcteur d'orthographe", "Spell checker",
            "Revenir dans le coin", "Back to his corner",
            "Lancer au démarrage de Windows", "Start with Windows",
            "Quitter", "Quit",
            "Remettre cet onglet à zéro", "Reset this tab",
            "Chercher : ", "Search: ",
            "Chercher par nom ou par famille", "Search by name or family",
            "▶ Jouer", "▶ Play",
            "Tout cocher", "Tick all",
            "Tout décocher", "Untick all",
            "À quelle fréquence il choisit chaque famille d'animations (0 = jamais).", "How often he picks each family of animations (0 = never).",
            "affichées", "shown",
            "animations actives sur", "active animations out of",
            "Double-clic pour en essayer une ; décochée, elle n'est plus choisie au hasard.", "Double-click to try one; unticked, it is no longer picked at random.",
            "Rouge", "Red", "Vert", "Green", "Bleu", "Blue", "Jaune", "Yellow", "Violet", "Purple", "Rose", "Pink", "Cyan", "Cyan", "Orange", "Orange",
            "Noir", "Black", "Rouge sombre", "Dark red", "Blanc", "White", "Gris", "Grey", "Turquoise", "Teal", "Or", "Gold", "Citron vert", "Lime", "Bleu nuit", "Navy",

            // ---------------------------------------------------------------- onglets et réglages
            "Apparence", "Appearance", "Mouvement", "Movement", "Comportement", "Behaviour", "Sons", "Sounds", "Souris", "Mouse", "Physique", "Physics",
            "Système", "System", "Familles", "Families", "Spécial", "Special",
            "Langue / Language", "Langue / Language",
            "comme Windows / same as Windows", "comme Windows / same as Windows",
            "Arc-en-ciel (change de couleur en continu)", "Rainbow (keeps changing colour)",
            "Vraies textures de Minecraft (lues dans le jeu installé sur ce PC)", "Real Minecraft textures (read from the game installed on this PC)",
            "Taille", "Size",
            "Épaisseur du trait", "Line thickness",
            "Taille de la tête", "Head size",
            "Longueur du torse", "Torso length",
            "Longueur des bras", "Arm length",
            "Longueur des jambes", "Leg length",
            "Épaisseur du contour", "Outline thickness",
            "Couleur du contour", "Outline colour",
            "Halo lumineux", "Glow",
            "Ombre au sol", "Ground shadow",
            "Opacité (%)", "Opacity (%)",
            "Traînée derrière la main", "Trail behind the hand",
            "Longueur de la traînée", "Trail length",
            "Vitesse de l'arc-en-ciel", "Rainbow speed",
            "Bulles de texte", "Speech bubbles",
            "Accessoires (épée, ballon, notes…)", "Props (sword, ball, notes…)",
            "selon la couleur, comme dans la série", "depends on the colour",
            "toujours pleine", "always solid",
            "toujours creuse (anneau)", "always a ring",
            "Vitesse des animations (%)", "Animation speed (%)",
            "Vitesse de déplacement (%)", "Walking speed (%)",
            "Douceur des transitions (ms)", "Transition smoothness (ms)",
            "Respiration au repos (%)", "Idle breathing (%)",
            "Images par seconde", "Frames per second",
            "Puissance des sauts (%)", "Jump power (%)",
            "Secondes entre deux actions", "Seconds between two actions",
            "Durée des animations (%)", "Animation length (%)",
            "Se promène", "Walks around",
            "Distance des promenades", "Walking distance",
            "Explore tout l'écran (sinon reste près de chez lui)", "Explores the whole screen (otherwise stays near home)",
            "Saute sur le haut des fenêtres", "Jumps onto the top of windows",
            "Envie de sauter sur une fenêtre (%)", "Urge to jump onto a window (%)",
            "Gestes pendant la marche (%)", "Gestures while walking (%)",
            "S'endort après (minutes sans toucher au PC, 0 = jamais)", "Falls asleep after (minutes without using the PC, 0 = never)",
            "Envie de se téléporter : commande, perle de l'Ender ou portail (%)", "Urge to teleport: command, Ender pearl or portal (%)",
            "Le premier stickman a le bâton de commande", "The first stickman carries the command staff",
            "Leur maison reste toujours visible", "Their house always stays visible",
            "CORRECTEUR : il corrige mes fautes d'orthographe (lit le texte près du curseur, jamais les mots de passe)", "SPELL CHECKER: he fixes my typos (reads the text next to the caret, never passwords)",
            "MODE FARCEUR : il ferme des fenêtres en appuyant sur leur croix", "PRANK MODE: he closes windows by pressing their X",
            "Mode farceur : minutes entre deux fermetures", "Prank mode: minutes between two closings",
            "Mode farceur : peut aussi fermer la fenêtre que j'utilise", "Prank mode: may also close the window I am using",
            "Sauts vers les fenêtres", "Jumps towards windows",
            "variés", "varied", "simples", "plain", "toujours en salto", "always a somersault", "atterrissage de héros", "hero landing",
            "Danse en rythme quand le PC joue de la musique (de temps en temps)", "Dances on the beat when the PC plays music (now and then)",
            "Envie de danser quand il y a de la musique (%)", "Urge to dance when music plays (%)",
            "Géant : il grandit d'un coup et fait fuir les autres", "Giant: he suddenly grows and scares the others away",
            "Minuscule : il rétrécit et file partout", "Tiny: he shrinks and scurries around",
            "Il dessine sur l'écran", "He draws on the screen",
            "Il ouvre YouTube sur la chaîne d'Alan Becker (ouvre le navigateur)", "He opens Alan Becker's YouTube channel (opens the browser)",
            "Il attrape le curseur au lasso (la souris bouge vraiment)", "He lassoes the mouse cursor (the mouse really moves)",
            "Il secoue la fenêtre où il est perché (la fenêtre bouge vraiment)", "He shakes the window he stands on (the window really moves)",
            "Nombre de stickmen", "Number of stickmen",
            "Ils vont se voir : bonjour, checks, câlins, duels amicaux…", "They visit each other: hello, fist bumps, hugs, friendly duels…",
            "Envie d'aller voir un ami (%)", "Urge to go and see a friend (%)",
            "Couleur du 2e", "Colour of the 2nd", "Couleur du 3e", "Colour of the 3rd", "Couleur du 4e", "Colour of the 4th", "Couleur du 5e", "Colour of the 5th", "Couleur du 6e", "Colour of the 6th",
            "Sons activés", "Sounds on",
            "Volume (%)", "Volume (%)",
            "Sauts, atterrissages, rebonds et chutes", "Jumps, landings, bounces and falls",
            "Quand on l'attrape, le lance ou le bouscule", "When grabbed, thrown or knocked over",
            "Bruitages des animations (coups, épée, énergie, saltos…)", "Animation sounds (hits, sword, energy, flips…)",
            "Téléportation", "Teleporting",
            "Suit le curseur du regard", "Watches the cursor",
            "Se bat avec le curseur quand il s'approche", "Fights the cursor when it comes close",
            "Distance de combat", "Fighting distance",
            "Pause entre deux coups (s)", "Pause between two hits (s)",
            "Suit le curseur", "Follows the cursor",
            "Fuit le curseur", "Runs away from the cursor",
            "Se fait renverser par un curseur rapide", "A fast cursor knocks him over",
            "On peut l'attraper à la souris", "Can be grabbed with the mouse",
            "On peut le lancer", "Can be thrown",
            "Un clic le fait", "A click makes him",
            "saluer", "wave", "sauter", "jump", "danser", "dance", "se battre", "fight", "une animation au hasard", "play a random animation",
            "Gravité (%)", "Gravity (%)",
            "Rebond (%)", "Bounce (%)",
            "Frottement au sol (%)", "Ground friction (%)",
            "Balancement quand on le porte (%)", "Swing when carried (%)",
            "Force du lancer (%)", "Throw strength (%)",
            "Vrilles en l'air (%)", "Spin in the air (%)",
            "Rebondit sur les bords de l'écran", "Bounces off the screen edges",
            "Toujours au premier plan", "Always on top",

            // ---------------------------------------------------------------- bulles
            "Salut !", "Hi!", "Hein ?!", "Huh?!", "Oups !", "Oops!", "Ouf !", "Phew!", "Aaaah !", "Aaaah!", "Aaah !", "Aaah!", "Aïe !", "Ouch!",
            "Yahou !", "Yahoo!", "Whoa !", "Whoa!", "Ooooh !", "Ooooh!", "Pfiou !", "Phew!", "Couic !", "Squeak!", "GRAOUH !", "ROAAAR!",
            "Hé !", "Hey!", "Hé hé hé…", "Heh heh heh…", "Facile.", "Easy.", "Il fait bon.", "Nice and warm.", "Élytres !", "Elytra!",
            "Regarde ça !", "Watch this!", "Ça secoue !", "Shake it!", "Elle résiste…", "It won't close…", "Trop tard…", "Too late…",
            "Me revoilà !", "I'm back!", "Ah, ça va mieux.", "Ah, that's better.", "Et voilà la maison !", "And there's the house!",
            "Pas mal, non ?", "Not bad, eh?", "Oh ! Une faute.", "Oh! A typo.", "« {0} », voilà !", "\"{0}\", there you go!",
            "Tiens, je te le rends.", "Here, have it back.", "Hé, viens par là !", "Hey, come over here!",
            "Solide, cette fenêtre.", "Sturdy window.", "Ouf, c'était grand là-haut.", "Phew, it was big up there.",
            "Déjà la nuit ? Bonne nuit…", "Night already? Good night…", "[Stickman] Salut tout le monde !", "[Stickman] Hello everyone!",
            "Allons voir la chaîne d'Alan Becker !", "Let's visit Alan Becker's channel!", "Pas de navigateur ?", "No browser?",
            "Il me faut le bâton de commande !", "I need the command staff!", "Il me faut une fenêtre !", "I need a window!",
            "Aucune fenêtre où m'asseoir", "No window to sit on", "Aucune fenêtre où sauter", "No window to jump onto",
            "Je ne trouve pas cette fenêtre", "I can't find that window", "Pas de fenêtre à secouer…", "No window to shake…",
            "Je n'entends pas de musique…", "I can't hear any music…", "Je suis tout seul…", "I'm all alone…",
            "Personne n'est libre", "Nobody is free", "Personne n'est libre…", "Nobody is free…", "Personne à appeler…", "Nobody to call…",
            "On est déjà six !", "There are six of us already!", "Chut… je ne fais plus de bruit", "Shh… I'll be quiet now", "Le son est revenu !", "Sound is back!",
            "Hé hé… gare à tes fenêtres !", "Heh heh… watch your windows!", "D'accord, je ne touche plus à rien", "All right, I won't touch anything",
            "Je surveille tes fautes (je ne garde rien de ce que tu écris)", "I'm watching your spelling (I keep nothing of what you type)",
            "D'accord, j'arrête de relire", "All right, I'll stop proofreading",

            // ---------------------------------------------------------------- familles
            "Déplacements", "Moving around", "Danses", "Dances", "Gestes", "Gestures", "Combat", "Fighting", "Acrobaties", "Acrobatics", "Sport", "Sport",
            "Quotidien", "Everyday life", "Émotions", "Emotions", "Fenêtres", "Windows", "Minecraft", "Minecraft", "Commandes", "Commands", "Décor", "Scenery",

            // ---------------------------------------------------------------- animations à deux
            "Bonjour", "Hello", "Hé, salut !", "Hey, hi!", "Check", "Fist bump", "Check !", "Bump!", "Tope-là", "High five", "Tope là !", "High five!", "Yeah !", "Yeah!",
            "Poignée de main", "Handshake", "Enchanté !", "Nice to meet you!", "Ça va ?", "How are you?", "Check complet", "Full handshake", "Yo !", "Yo!", "Trop stylé !", "So cool!",
            "Câlin !", "Hug!", "Pierre-feuille-ciseaux", "Rock paper scissors", "Pierre, feuille, ciseaux !", "Rock, paper, scissors!", "…ciseaux !", "…scissors!",
            "Gagné !", "I win!", "Oh non…", "Oh no…", "Duel amical", "Friendly duel", "En garde !", "En garde!", "Viens !", "Come on!", "Bien joué !", "Well played!",
            "Coude contre coude", "Elbow bump", "Coude !", "Elbow!", "Double tope", "Double high five", "Des deux mains !", "Both hands!", "Allez !", "Go!", "Encore !", "Again!",
            "Révérence", "Bow", "Après vous.", "After you.", "Mais non, après vous !", "No, no, after you!", "Danse à deux", "Dance together", "On danse ?", "Shall we dance?", "Carrément !", "Totally!",

            // ---------------------------------------------------------------- danses : bras, puis jambes
            "Boxe", "Boxing", "Bras qui se balancent", "Swaying arms", "Brasse", "Breaststroke", "Cadres", "Frames", "Clap haut-bas", "Clap high and low", "Dab", "Dab",
            "Déhanché", "Hip sway", "Disco", "Disco", "Égyptien", "Egyptian", "Essuie-glaces", "Wipers", "Fièvre du samedi soir", "Saturday night", "Floss", "Floss",
            "Guitare", "Air guitar", "Hélicoptère", "Helicopter", "Lasso", "Lasso", "Macarena", "Macarena", "Mains en l'air", "Hands up", "Manivelle", "Crank",
            "Maracas", "Maracas", "Moulinets", "Windmills", "Papillon", "Butterfly", "Poings en l'air", "Fists up", "Pointe gauche-droite", "Point left and right",
            "Pom-pom", "Pom-poms", "Poulet", "Chicken", "Robot", "Robot", "Roulé d'épaules", "Shoulder roll", "Rouleau", "Roll", "Tape des cuisses", "Thigh slap",
            "Tape des mains", "Clap", "Twist des bras", "Arm twist", "Vague", "Wave", "Vague à deux bras", "Two-arm wave", "YMCA", "YMCA",
            "coups de pied", "kicks", "course sur place", "running on the spot", "pas chassés", "side steps", "rebond", "bounce", "sauts", "jumps", "squats", "squats",
            "talons", "heels", "twist", "twist",

            // ---------------------------------------------------------------- animations
            "/effect : lévitation", "/effect: levitation", "/effect : vitesse", "/effect: speed", "/fill : fait apparaître la maison", "/fill: makes the house appear",
            "/gamemode : vole en créatif", "/gamemode: flies in creative", "/give : une épée en diamant", "/give: a diamond sword", "/particle : des cœurs", "/particle: hearts",
            "/say : salue tout le monde", "/say: greets everyone", "/setblock : pose un bloc", "/setblock: places a block", "/summon : la foudre", "/summon: lightning",
            "/summon : un feu d'artifice", "/summon: a firework", "/summon : une TNT", "/summon: TNT", "/time : la nuit", "/time: night", "/tp : se téléporte", "/tp: teleports",
            "/tp @a : appelle toute la bande", "/tp @a: calls the whole gang", "/weather : la pluie", "/weather: rain",
            "À cloche-pied", "Hopping on one foot", "À genoux", "Kneeling", "A le hoquet", "Has the hiccups", "À quatre pattes", "On all fours", "Abdos", "Sit-ups",
            "Admire un diamant", "Admires a diamond", "Allongé au bord, un bras dans le vide", "Lying on the edge, one arm dangling", "Allongé, bras sous la tête", "Lying down, arms behind his head",
            "Amour", "Love", "Applaudit", "Claps", "Applaudit en sautillant", "Claps while hopping", "Applaudit lentement", "Slow clap", "Arrose des fleurs", "Waters flowers",
            "Arroseur automatique", "Sprinkler", "Assis au bord, balance les jambes", "Sitting on the edge, swinging his legs", "Assis au bord, rêvasse", "Sitting on the edge, daydreaming",
            "Assis jambes tendues", "Sitting with legs stretched", "Assis, balance les pieds", "Sitting, swinging his feet", "Attend le bus", "Waits for the bus",
            "Attrape le curseur au lasso", "Lassoes the cursor", "Avance à la lanterne", "Walks with a lantern", "Bâille", "Yawns", "Balaie", "Sweeps", "Balayage", "Leg sweep",
            "Balaye de la main", "Brushes it off", "Balayette", "Low sweep", "Bâtit la maison de ses mains", "Builds the house by hand", "Bâton : frappe au sol", "Staff: ground strike",
            "Bâton : tourbillon", "Staff: whirlwind", "Bêche son jardin", "Hoes his garden", "Boit", "Drinks", "Boit un seau de lait", "Drinks a bucket of milk", "Boit une potion", "Drinks a potion",
            "Boitille", "Limps", "Bombe le torse", "Puffs out his chest", "Bonds de grenouille, de plus en plus haut", "Frog leaps, higher and higher", "Bouclier", "Shield", "Boude", "Sulks",
            "Boude, bras croisés", "Sulks, arms crossed", "Boule d'énergie", "Energy ball", "Bowling", "Bowling", "Boxe à vide", "Shadow punches", "Brandit un totem", "Raises a totem",
            "Bras croisés", "Arms crossed", "Bras croisés, tape du pied", "Arms crossed, tapping his foot", "Brasse à sec", "Dry breaststroke", "Burpee", "Burpee", "Câlin", "Hug",
            "Caprice", "Tantrum", "Chaise invisible", "Wall sit", "Chandelle", "Shoulder stand", "Chante au micro", "Sings into a mic", "Charge", "Charge", "Charge de l'épaule", "Shoulder charge",
            "Cherche ses clés dans ses poches", "Looks for his keys", "Choqué", "Shocked", "Chut !", "Shh!", "Claque des doigts", "Snaps his fingers", "Cœur avec les bras", "Heart with his arms",
            "Colère", "Anger", "Combo à l'épée en diamant", "Diamond sword combo", "Compte jusqu'à trois", "Counts to three", "Compte ses émeraudes", "Counts his emeralds",
            "Compte sur ses doigts", "Counts on his fingers", "Concentre son énergie", "Gathers his energy", "Confusion", "Confused", "Corde à sauter", "Jump rope",
            "Coucou à deux mains", "Two-handed wave", "Coucou des deux mains", "Waves with both hands", "Coup de chapeau", "Tips his hat", "Coup de coude", "Elbow strike",
            "Coup de genou", "Knee strike", "Coup de marteau des deux poings", "Double hammer fist", "Coup de pied", "Kick", "Coup de pied arrière", "Back kick", "Coup de pied bas", "Low kick",
            "Coup de pied en ciseaux", "Scissor kick", "Coup de pied en vrille", "Spinning kick", "Coup de pied haut", "High kick", "Coup de pied retourné", "Roundhouse kick",
            "Coup de pied sauté", "Jump kick", "Coup de poing", "Punch", "Coup de poing bas", "Low punch", "Coup de poing haut", "High punch", "Coup de tête", "Headbutt",
            "Coupe du bois à la hache", "Chops wood with an axe", "Course", "Run", "Course au ralenti", "Slow-motion run", "Course en arrière", "Backward run", "Course ninja", "Ninja run",
            "Course paniquée", "Panicked run", "Crawl à sec", "Dry front crawl", "Creuse à la pelle", "Digs with a shovel", "Crochet", "Hook", "Croque une carotte dorée", "Bites a golden carrot",
            "Croque une pomme dorée", "Bites a golden apple", "Danse de la joie", "Happy dance", "Danse de la victoire", "Victory dance", "Demande la parole", "Raises his hand",
            "Démarche cool", "Cool walk", "Démarche de cow-boy", "Cowboy walk", "Départ de sprint", "Sprint start", "Dépité", "Dejected", "Désespoir", "Despair",
            "Désigne quelqu'un du doigt", "Points at someone", "Dessine", "Draws", "Dessine sur l'écran", "Draws on the screen", "Développé au-dessus de la tête", "Overhead press",
            "Devient géant", "Turns giant", "Devient minuscule", "Turns tiny", "Direct en reculant", "Jab while backing off", "DJ", "DJ", "Dort", "Sleeps", "Dort dans un lit", "Sleeps in a bed",
            "Double coup de poing", "Double punch", "Double poing", "Double fist", "Double salto arrière", "Double backflip", "Double salto avant", "Double front flip", "Double saut", "Double jump",
            "Dribble", "Dribble", "Dribble du pied", "Foot dribble", "Enchaînement de 2 coups", "2-hit combo", "Enchaînement de 3 coups", "3-hit combo", "Enchaînement de 5 coups", "5-hit combo",
            "Enchaînement gauche-droite-uppercut", "Left-right-uppercut combo", "Ennui", "Bored", "Envoie un bisou", "Blows a kiss", "Envoie un bisou des deux mains", "Blows a kiss with both hands",
            "Épée : double taille", "Sword: double slash", "Épée : estoc", "Sword: thrust", "Épée : moulinet", "Sword: flourish", "Épée : parade", "Sword: parry",
            "Épée : parade et riposte", "Sword: parry and riposte", "Épée : taille haute", "Sword: high slash", "Épée : taille montante", "Sword: rising slash", "Équerre", "L-sit",
            "Équilibre sur la tête", "Headstand", "Équilibre sur une main", "One-hand handstand", "Escalier de blocs", "Block staircase", "Esquive arrière", "Back dodge", "Esquive basse", "Duck",
            "Esquive en pivotant", "Pivot dodge", "Esquives de boxeur", "Boxer's weave", "Essuie une vitre", "Wipes a window", "Éternue", "Sneezes", "Étire ses quadriceps", "Stretches his quads",
            "Étirement latéral", "Side stretch", "Explore à la torche", "Explores with a torch", "Explose de rire, plié en deux", "Doubles over laughing", "Fabrique une épée sur l'établi", "Crafts a sword at the crafting table",
            "Facepalm", "Facepalm", "Fait craquer ses doigts", "Cracks his knuckles", "Fait des pompes de canard", "Chicken wings", "Fait du stop", "Hitchhikes", "Fait du vélo sur le dos", "Bicycle kicks on his back",
            "Fait la planche contre un mur", "Leans against a wall", "Fait la sieste à la maison", "Takes a nap at home", "Fait la statue", "Plays statue", "Fait la vaisselle", "Does the dishes",
            "Fait l'avion", "Plays airplane", "Fait le signe de la paix", "Peace sign", "Fait le signe du cœur avec les doigts", "Finger heart", "Fait les poussières", "Dusts",
            "Fait non du doigt", "Wags his finger", "Fait ses lacets", "Ties his shoelaces", "Fait signe d'approcher", "Beckons", "Fait signe de partir", "Waves someone away",
            "Fait tournoyer son bâton", "Twirls his staff", "Fait un cadre avec ses mains", "Frames a shot with his hands", "Fentes", "Lunges", "Fentes latérales", "Side lunges",
            "Fentes sautées", "Jump lunges", "Feu d'artifice", "Firework", "Feu de camp", "Campfire", "Fier", "Proud", "Fier comme un coq", "Proud as a peacock",
            "Flâne les mains dans le dos", "Strolls, hands behind his back", "Flip arrière groupé", "Tucked backflip", "Flip-flap", "Back handspring", "Fonce tête baissée", "Charges head down",
            "Fond en larmes", "Bursts into tears", "Fou rire", "Giggle fit", "Freeze de breakdance", "Breakdance freeze", "Frissonne", "Shivers", "Frissonne de froid", "Shivers with cold",
            "Fuite paniquée", "Panicked escape", "Funambule", "Tightrope walk", "Gainage sur un bras", "One-arm plank", "Garde à l'épée", "Sword guard", "Garde basse", "Low guard",
            "Garde basse, feintes", "Low guard, feints", "Garde haute", "High guard", "Garde haute, pas chassés", "High guard, side steps", "Genoux hauts", "High knees",
            "Glissade sur les genoux", "Knee slide", "Glisse et tombe", "Slips and falls", "Grand écart", "Splits", "Grand écart sauté", "Split jump", "Grand jeté", "Grand jeté",
            "Grand salut", "Big wave", "Grand saut", "Big jump", "Grandes enjambées", "Long strides", "Grands moulinets des bras", "Big arm circles", "Grignote un cookie", "Nibbles a cookie",
            "Grimpe", "Climbs", "Grimpe à un mur invisible", "Climbs an invisible wall", "Haltères", "Dumbbells", "Haltérophilie", "Weightlifting", "Hausse les épaules", "Shrugs",
            "Hausse les épaules deux fois", "Shrugs twice", "Hésite, se balance d'un pied sur l'autre", "Hesitates, shifting from foot to foot", "Hoche la tête", "Nods", "Hula hoop", "Hula hoop",
            "Idée !", "Idea!", "Impatient", "Impatient", "Jogging tranquille", "Easy jog", "Joie", "Joy", "Jongle", "Juggles", "Jongle avec un ballon", "Keepy-uppies",
            "Joue à la console", "Plays video games", "Joue au yo-yo", "Plays with a yo-yo", "Joue de la guitare", "Plays the guitar", "Joue du tambour", "Plays the drum",
            "Jubile en silence", "Silent cheer", "Jumelles", "Binoculars", "Jumping jacks", "Jumping jacks", "Kata de karaté", "Karate kata", "Kip-up", "Kip-up",
            "Lance des confettis", "Throws confetti", "Lance le trident", "Throws the trident", "Lance un shuriken", "Throws a shuriken", "Lance une boule de neige", "Throws a snowball",
            "Lance une pièce", "Flips a coin", "Lancer", "Throw", "Lancer de javelot", "Javelin throw", "Lancer de poids", "Shot put", "Lancers francs", "Free throws", "Le ver", "The worm",
            "Lève les bras au ciel", "Throws his arms up", "Lit", "Reads", "Lit le journal assis", "Reads the paper, sitting", "Lit un livre enchanté", "Reads an enchanted book", "Lit une carte", "Reads a map",
            "Mains dans le dos", "Hands behind his back", "Mains en porte-voix", "Shouts through cupped hands", "Mains sur les hanches", "Hands on hips", "Mains sur les hanches (marche)", "Hands on hips (walking)",
            "Mange", "Eats", "Mange du pain", "Eats bread", "Mange un steak", "Eats a steak", "Marche", "Walk", "Marche accroupie", "Crouch walk", "Marche arrière", "Walks backwards",
            "Marche au pas cadencé", "Quick march", "Marche de mannequin", "Catwalk", "Marche décontractée", "Relaxed walk", "Marche lente", "Slow walk", "Marche lourde", "Heavy walk",
            "Marche militaire", "Military march", "Marche pressée, penché en avant", "Hurried walk, leaning forward", "Marche rapide", "Fast walk", "Marche sur des œufs", "Walks on eggshells",
            "Marche sur les genoux", "Walks on his knees", "Marche sur les mains", "Walks on his hands", "Marteau géant", "Giant hammer", "Méditation", "Meditation", "Mime une vitre", "Mimes a glass wall",
            "Mine avec une pioche", "Mines with a pickaxe", "Montées de genoux", "Knee raises", "Montre ses biceps", "Flexes his biceps", "Montre ses muscles", "Shows off his muscles",
            "Moonwalk", "Moonwalk", "Moulin à vent", "Windmill", "Moulin à vent, mains aux pieds", "Windmill toe touches", "Mouline d'un seul bras", "One-arm windmill",
            "Moulinet du poignet", "Wrist twirl", "Moulinets de hanches", "Hip circles", "Moulinets de poings", "Fist circles", "Mountain climbers", "Mountain climbers", "Nage", "Swims",
            "Non du doigt", "Finger wag", "Noue sa cravate", "Ties his tie", "Nunchaku", "Nunchaku", "Observe à la longue-vue", "Looks through a spyglass", "Onde de choc", "Shockwave",
            "Ouvre YouTube sur la chaîne d'Alan Becker", "Opens Alan Becker's YouTube channel", "Panique, court sur place", "Panics, running on the spot", "Parade haute puis basse", "High then low block",
            "Parapluie", "Umbrella", "Pas de géant", "Giant steps", "Pas de géant, lents", "Slow giant steps", "Pas de loup", "Tiptoes stealthily", "Pas de parade", "Parade step",
            "Passe l'aspirateur", "Vacuums", "Patinage", "Skating", "Pêche depuis le bord", "Fishes from the edge", "Pédale", "Pedals", "Peint un mur au rouleau", "Paints a wall with a roller",
            "Penalty", "Penalty kick", "Perle de l'Ender", "Ender pearl", "Petit trot", "Trot", "Petits pas pressés", "Quick little steps", "Peur", "Fear", "Planche", "Plank",
            "Planche faciale", "Front lever", "Plante des fleurs", "Plants flowers", "Pleure", "Cries", "Plongeon roulé", "Dive roll", "Pluie de coups de pied", "Flurry of kicks",
            "Poing du dragon", "Dragon punch", "Poing levé", "Raised fist", "Pointe derrière", "Points behind", "Pointe devant", "Points ahead", "Pointe en haut", "Points up",
            "Poirier", "Handstand", "Pompes", "Push-ups", "Pompes en équilibre", "Handstand push-ups", "Pompes sur un bras", "One-arm push-ups", "Pont arrière", "Back bridge",
            "Portail du Nether", "Nether portal", "Porte un carton lourd", "Carries a heavy box", "Porte un plateau", "Carries a tray", "Porte un toast", "Raises a toast",
            "Pouce en l'air", "Thumbs up", "Pouces en l'air des deux mains", "Two thumbs up", "Prend un selfie", "Takes a selfie", "Prend une photo", "Takes a photo",
            "Prise et projection", "Grab and throw", "Provocation", "Taunt", "Rafale de coups de poing", "Flurry of punches", "Rafale de directs", "Flurry of jabs", "Rage", "Rage",
            "Ramasse quelque chose", "Picks something up", "Ramasse quelque chose par terre", "Picks something up off the ground", "Rame en bateau", "Rows a boat", "Rameur", "Rowing machine",
            "Rampe", "Crawls", "Rayon d'énergie", "Energy beam", "Réfléchit", "Thinks", "Regarde au loin", "Looks into the distance", "Regarde en bas", "Looks down",
            "Regarde le ciel", "Looks at the sky", "Regarde l'heure", "Checks the time", "Regarde sa boussole", "Checks his compass", "Regarde sa montre", "Checks his watch",
            "Regarde ses pieds", "Looks at his feet", "Rentre à la maison", "Goes home", "Repasse une chemise", "Irons a shirt", "Reprend son souffle", "Catches his breath",
            "Rêvasse, la tête dans les nuages", "Daydreams, head in the clouds", "Révérence profonde", "Deep bow", "Rit", "Laughs", "Rondade flip", "Round-off flip", "Roue", "Cartwheel",
            "Roue arrière", "Back cartwheel", "Roue sans les mains", "No-hands cartwheel", "Roue sur un bras", "One-arm cartwheel", "Roulade arrière", "Backward roll", "Roulade avant", "Forward roll",
            "Roulade d'esquive", "Dodge roll", "Roule en boule", "Rolls into a ball", "Roule en wagonnet", "Rides a minecart", "Salto arrière", "Backflip", "Salto avant", "Front flip",
            "Salto tendu", "Layout flip", "Salto vrillé", "Twisting flip", "Salue comme une reine", "Royal wave", "Salut", "Wave", "Salut du combattant", "Fighter's bow",
            "Salut martial", "Martial salute", "Salut militaire", "Military salute", "Salut timide", "Shy wave", "S'appuie sur son bâton", "Leans on his staff", "S'assoit", "Sits down",
            "Saut", "Jump", "Saut carpé", "Pike jump", "Saut carpé jambes écartées", "Straddle jump", "Saut de chat", "Cat leap", "Saut de l'ange", "Swan dive", "Saut de main", "Handspring",
            "Saut de mains", "Front handspring", "Saut écart", "Star split jump", "Saut en étoile", "Star jump", "Saut en longueur", "Long jump", "Saut en longueur sur place", "Standing long jump",
            "Saut groupé", "Tuck jump", "Saut groupé-tendu", "Tuck then stretch jump", "Saute à la corde invisible", "Skips an invisible rope", "Saute de joie en tournant", "Jumps for joy, spinning",
            "Saute de joie, bras en V", "Jumps for joy, arms in a V", "Saute de surprise et retombe assis", "Jumps in surprise and lands sitting", "Sautille", "Hops", "Sautille d'excitation", "Bounces with excitement",
            "Sautille en garde", "Bounces on guard", "Sauts de grenouille", "Frog jumps", "Se balance", "Sways", "Se brosse les dents", "Brushes his teeth", "Se cache les yeux", "Covers his eyes",
            "Se chauffe les mains", "Warms his hands", "Se frotte le ventre", "Rubs his belly", "Se frotte les mains", "Rubs his hands", "Se frotte les yeux", "Rubs his eyes",
            "Se gratte la nuque", "Scratches his neck", "Se gratte la tête", "Scratches his head", "Se gratte le menton", "Scratches his chin", "Se prend la tête à deux mains", "Holds his head in both hands",
            "Se recoiffe", "Fixes his hair", "Se ronge les ongles", "Bites his nails", "Se tape le front", "Slaps his forehead", "Seau d'eau au dernier moment", "Last-second water bucket",
            "Secoue sa fenêtre", "Shakes his window", "Service de tennis", "Tennis serve", "S'étire", "Stretches", "S'étire au réveil", "Wake-up stretch", "S'évente de la main", "Fans himself",
            "Shadow-boxing en sautillant", "Bouncing shadow-boxing", "Shoote dans un caillou", "Kicks a pebble", "Shuffle", "Shuffle", "Sieste sur le ventre", "Naps on his belly", "Siffle", "Whistles",
            "S'incline", "Bows", "S'incline, vaincu", "Bows, defeated", "Skateboard", "Skateboard", "Ski (schuss)", "Skiing (tuck)", "Smash de volley", "Volleyball spike",
            "Sonne la cloche", "Rings the bell", "Souffle dans une corne", "Blows a horn", "Soulève un chapeau invisible", "Tips an invisible hat", "Soupir de soulagement", "Sigh of relief",
            "Soupire", "Sighs", "Sprint", "Sprint", "Sprint à fond", "Full sprint", "Squats", "Squats", "Squats sautés", "Jump squats", "Stop", "Stop", "Supplie", "Begs",
            "Sur la pointe des pieds", "On tiptoe", "Surexcité", "Overexcited", "Surf", "Surfing", "Surprise", "Surprise", "Sursaute de peur", "Jumps with fright", "Swing de golf", "Golf swing",
            "Tai-chi", "Tai chi", "Talons-fesses", "Butt kicks", "Tape au clavier", "Types on a laptop", "Tape des mains au-dessus de la tête", "Claps above his head", "Tape du pied", "Taps his foot",
            "Tape du poing dans sa main", "Pounds his fist into his palm", "Tape sur un clavier debout", "Types standing up", "Tape un arbre à mains nues", "Punches a tree", "Tape un texto", "Texts",
            "Téléphone", "On the phone", "Tend l'oreille", "Cups his ear", "Timide", "Shy", "Timide, se tortille", "Shy, squirming", "Tir à l'arc", "Archery", "Tir au but", "Shot on goal",
            "Tir au panier", "Jump shot", "Titube", "Staggers", "TNT", "TNT", "Tombe à la renverse de surprise", "Falls over backwards in surprise", "Touche ses orteils", "Touches his toes",
            "Touche ses pieds", "Reaches for his feet", "Toupie de combat", "Spinning attack", "Toupie sur la tête", "Head spin", "Tour de blocs", "Block tower", "Tousse", "Coughs",
            "Tractions", "Pull-ups", "Trampoline de slime", "Slime trampoline", "Trébuche", "Trips", "Tremble de peur", "Trembles with fear", "Trépigne d'impatience", "Stamps impatiently",
            "Triomphe à genoux", "Triumphs on his knees", "Triple salto", "Triple flip", "Tristesse", "Sadness", "Trotte en sifflotant", "Trots along whistling", "Trottine sur la pointe", "Trots on tiptoe",
            "Twist des genoux", "Knee twist", "Uppercut", "Uppercut", "Uppercut sauté", "Jumping uppercut", "V de la victoire", "V for victory", "Vague du corps", "Body wave", "Victoire", "Victory",
            "Viens ici", "Come here", "Visse une ampoule", "Screws in a light bulb", "Vol en élytres", "Elytra flight", "Vole comme un super-héros", "Flies like a superhero", "Yes !", "Yes!",
            "Yoga : chien tête en bas", "Yoga: downward dog", "Yoga : la chandelle", "Yoga: shoulder stand", "Yoga : l'arbre", "Yoga: tree", "Yoga : le cobra", "Yoga: cobra",
            "Yoga : le guerrier", "Yoga: warrior", "Zombie", "Zombie",
        };
    }
}
