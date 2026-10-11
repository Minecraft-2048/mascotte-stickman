// Paroles du stickman : de courtes phrases qu'il écrit à l'écran, lettre après lettre (voir Bonhomme.Parler).
// Par défaut elles sont composées ici, sans aucun réseau. On peut aussi les demander à un modèle de langage
// (réglage « parleur ») :
//   - Claude, par l'API d'Anthropic. La clé se lit dans la variable d'environnement ANTHROPIC_API_KEY, sinon
//     dans le fichier cle-anthropic.txt du dossier des réglages ; le programme ne l'écrit nulle part.
//     Modèle : claude-opus-5-5, ou celui que nomme le fichier modele-claude.txt.
//   - Ollama : un modèle qui tourne sur ce PC (http://localhost:11434), sans clé et sans sortir du PC.
// Chaque phrase est unique : tout ce qu'il a écrit est noté dans paroles-dites.txt (dossier des réglages) et
// ne revient jamais, d'un lancement à l'autre non plus.
// Ce qui part vers le modèle, et rien d'autre : la consigne (qui il est), l'heure et le jour, le nom de sa
// dernière animation, le nombre de stickmen et de fenêtres ouvertes (pas leurs titres), s'il y a de la musique,
// et ses douze dernières phrases. Rien de l'écran, rien de ce que tape l'utilisateur. Si le modèle ne répond
// pas à temps, il écrit une phrase composée ici.
// Pas de SDK : le projet se compile avec le csc de Windows (C# 5, .NET Framework 4), d'où l'appel HTTP direct.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace MascotteStickman
{
    static class Paroles
    {
        const string ModeleParDefaut = "claude-opus-5-5";
        static readonly object verrou = new object();
        static readonly Random hasard = new Random();
        static readonly JavaScriptSerializer json = new JavaScriptSerializer();
        public static volatile string DerniereErreur;            // pourquoi le modèle n'a pas répondu (affiché dans les réglages)

        // Ce que le stickman sait de lui au moment de parler : c'est ce qui rend ses phrases de circonstance.
        public sealed class Contexte
        {
            public string Animation;                 // la dernière animation qu'il a jouée (nom français)
            public int Amis, Fenetres, Animations;   // combien d'autres stickmen, de fenêtres où grimper, d'animations connues
            public bool Musique, Perche;
            public bool Arrivee;                     // le programme vient d'être lancé : il commence par saluer
        }

        static readonly HashSet<string> dites = new HashSet<string>(StringComparer.OrdinalIgnoreCase);      // tout ce qu'il a déjà écrit : jamais deux fois
        static readonly List<string> recentes = new List<string>();
        static bool lues;

        static string Journal { get { return Path.Combine(Programme.Dossier, "paroles-dites.txt"); } }

        static void LireJournal()
        {
            if (lues) return;
            lues = true;
            try { if (File.Exists(Journal)) foreach (string ligne in File.ReadAllLines(Journal, Encoding.UTF8)) if (ligne.Length > 0) { dites.Add(ligne); recentes.Add(ligne); } }
            catch (IOException) { }
        }

        static void Noter(string phrase)
        {
            dites.Add(phrase);
            recentes.Add(phrase);
            try
            {
                if (recentes.Count > 3000) { recentes.RemoveRange(0, 1000); File.WriteAllLines(Journal, recentes, Encoding.UTF8); }      // le journal ne grossit pas sans fin
                else File.AppendAllText(Journal, phrase + Environment.NewLine, Encoding.UTF8);
            }
            catch (IOException) { }
        }

        public static bool AvecModele { get { return (int)R.D("parleur") != 0; } }

        // Une phrase composée ici, tout de suite, à partir de ce qu'il vient de faire, de l'heure, de ses amis… et
        // qu'il n'a jamais écrite.
        public static string Composee(Contexte c)
        {
            lock (verrou) { LireJournal(); return Integree(Langue.Anglais, c); }
        }

        // Demande au modèle une phrase pour ce moment précis. « suite » la reçoit sur un fil de travail, ou reçoit
        // null si le modèle n'a pas répondu ou s'il a redonné une phrase déjà écrite (on en composera une ici).
        public static void Demander(Contexte c, Action<string> suite)
        {
            int source = (int)R.D("parleur");
            bool anglais = Langue.Anglais;
            ThreadPool.QueueUserWorkItem(o =>
            {
                string p = null;
                try
                {
                    string consigne;
                    lock (verrou) { LireJournal(); consigne = Consigne(anglais, c); }
                    p = Nettoyer(source == 1 ? DemanderClaude(consigne, anglais) : DemanderOllama(consigne, anglais));
                    DerniereErreur = p == null ? "réponse vide" : null;
                    lock (verrou) if (p != null && dites.Contains(p)) p = null;
                }
                catch (Exception e) { DerniereErreur = e.Message; p = null; }
                suite(p);
            });
        }

        // Il vient de l'écrire : on s'en souvient (d'un lancement à l'autre aussi) pour ne plus jamais la redire.
        public static void Retenir(string phrase)
        {
            lock (verrou) { LireJournal(); Noter(phrase); }
        }

        // La consigne donnée au modèle : qui il est, ce qu'il vient de faire, et ce qu'il a déjà dit (pour ne pas se répéter).
        static string Consigne(bool anglais, Contexte c)
        {
            DateTime d = DateTime.Now;
            var s = new StringBuilder();
            if (anglais)
            {
                s.Append("You are a small orange stick figure who lives on someone's computer desktop: you walk along the taskbar, climb onto windows, dance, do flips, and write short messages on the screen with your finger. ");
                s.Append("Write ONE short sentence (14 words at most) for the person at the computer, in your own voice. It must be new: never reuse or rephrase a sentence from the list below. ");
                s.Append("Use what is going on right now. It is " + d.ToString("HH:mm") + " on " + d.ToString("dddd", CultureInfo.GetCultureInfo("en-US")) + ". ");
                if (c.Arrivee) s.Append("The program has just been started, so begin by greeting the person. ");
                if (c.Animation != null) s.Append("You have just done this: \"" + Langue.EnAnglais(c.Animation) + "\". ");
                s.Append(c.Amis == 0 ? "You are alone on the screen. " : "There are " + (c.Amis + 1) + " stick figures on the screen, you and your friends. ");
                if (c.Musique) s.Append("Music is playing on the computer. ");
                if (c.Perche) s.Append("You are standing on top of a window. ");
                if (c.Fenetres > 0) s.Append(c.Fenetres + " windows are open. ");
                s.Append("You know " + c.Animations + " animations. Reply with the sentence only: no quotation marks, no emoji, no explanation.");
                if (recentes.Count > 0) s.Append(" Sentences you have already written:");
            }
            else
            {
                s.Append("Tu es un petit bonhomme-bâton orange qui vit sur le bureau de l'ordinateur de quelqu'un : tu marches sur la barre des tâches, tu grimpes sur les fenêtres, tu danses, tu fais des saltos, et tu écris de courts messages sur l'écran avec ton doigt. ");
                s.Append("Écris UNE seule phrase courte (14 mots au plus) pour la personne devant l'ordinateur, avec ta voix à toi. Elle doit être nouvelle : ne reprends et ne reformule aucune phrase de la liste ci-dessous. ");
                s.Append("Sers-toi de ce qui se passe en ce moment. Il est " + d.ToString("HH:mm") + ", " + d.ToString("dddd", CultureInfo.GetCultureInfo("fr-FR")) + ". ");
                if (c.Arrivee) s.Append("Le programme vient d'être lancé : commence par saluer la personne. ");
                if (c.Animation != null) s.Append("Tu viens de faire ceci : « " + c.Animation + " ». ");
                s.Append(c.Amis == 0 ? "Tu es seul sur l'écran. " : "Vous êtes " + (c.Amis + 1) + " bonshommes-bâtons sur l'écran, toi et tes amis. ");
                if (c.Musique) s.Append("De la musique joue sur l'ordinateur. ");
                if (c.Perche) s.Append("Tu es perché sur le haut d'une fenêtre. ");
                if (c.Fenetres > 0) s.Append(c.Fenetres + " fenêtres sont ouvertes. ");
                s.Append("Tu connais " + c.Animations + " animations. Réponds par la phrase seule : sans guillemets, sans emoji, sans explication.");
                if (recentes.Count > 0) s.Append(" Phrases que tu as déjà écrites :");
            }
            for (int i = Math.Max(0, recentes.Count - 12); i < recentes.Count; i++) s.Append("\n- " + recentes[i]);
            return s.ToString();
        }

        // ---------------------------------------------------------------- Claude (API d'Anthropic)

        static string DemanderClaude(string consigne, bool anglais)
        {
            string cle = Lire("ANTHROPIC_API_KEY", "cle-anthropic.txt");
            if (cle == null) throw new InvalidOperationException("clé d'API absente (variable ANTHROPIC_API_KEY ou fichier cle-anthropic.txt)");
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;      // TLS 1.2, que .NET 4 n'active pas tout seul
            string racine = (Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL") ?? "https://api.anthropic.com").TrimEnd('/');
            // la réflexion reste adaptative (on ne peut pas la couper sur ce modèle) : un effort bas suffit pour une phrase.
            // « fallbacks » : si le modèle décline, un autre répond à sa place côté serveur.
            string corps = "{\"model\":" + json.Serialize(Lire(null, "modele-claude.txt") ?? ModeleParDefaut)
                + ",\"max_tokens\":2000,\"fallbacks\":\"default\",\"output_config\":{\"effort\":\"low\"}"
                + ",\"system\":" + json.Serialize(consigne)
                + ",\"messages\":[{\"role\":\"user\",\"content\":" + json.Serialize(anglais ? "Write your sentence." : "Écris ta phrase.") + "}]}";
            var reponse = (Dictionary<string, object>)json.DeserializeObject(Poster(racine + "/v1/messages", corps,
                "x-api-key", cle, "anthropic-version", "2023-06-01", "anthropic-beta", "server-side-fallback-2026-07-01"));
            object arret;
            if (reponse.TryGetValue("stop_reason", out arret) && "refusal".Equals(arret)) return null;      // décliné malgré tout : phrase d'ici
            foreach (object bloc in (object[])reponse["content"])                                              // le premier bloc de texte (les autres : réflexion, relais)
            {
                var b = bloc as Dictionary<string, object>;
                if (b != null && "text".Equals(b["type"])) return b["text"] as string;
            }
            return null;
        }

        // ---------------------------------------------------------------- Ollama (modèle sur ce PC)

        static string DemanderOllama(string consigne, bool anglais)
        {
            string racine = (Environment.GetEnvironmentVariable("OLLAMA_HOST") ?? "http://localhost:11434").TrimEnd('/');
            if (!racine.StartsWith("http", StringComparison.OrdinalIgnoreCase)) racine = "http://" + racine;
            string modele = Lire(null, "modele-ollama.txt");
            if (modele == null)                                  // pas de fichier : le premier modèle installé
            {
                var liste = (Dictionary<string, object>)json.DeserializeObject(Requete(racine + "/api/tags", null));
                var modeles = liste["models"] as object[];
                if (modeles == null || modeles.Length == 0) throw new InvalidOperationException("aucun modèle Ollama installé");
                modele = ((Dictionary<string, object>)modeles[0])["name"] as string;
            }
            string corps = "{\"model\":" + json.Serialize(modele) + ",\"stream\":false,\"system\":" + json.Serialize(consigne)
                + ",\"prompt\":" + json.Serialize(anglais ? "Write your sentence." : "Écris ta phrase.") + "}";
            var reponse = (Dictionary<string, object>)json.DeserializeObject(Poster(racine + "/api/generate", corps));
            return reponse["response"] as string;
        }

        // ---------------------------------------------------------------- HTTP

        static string Poster(string adresse, string corps, params string[] entetes) { return Requete(adresse, corps, entetes); }

        static string Requete(string adresse, string corps, params string[] entetes)
        {
            var requete = (HttpWebRequest)WebRequest.Create(adresse);
            requete.Timeout = 20000; requete.ReadWriteTimeout = 20000;      // il attend la réponse en réfléchissant : pas trop longtemps
            if (entetes != null) for (int i = 0; i + 1 < entetes.Length; i += 2) requete.Headers[entetes[i]] = entetes[i + 1];
            if (corps != null)
            {
                byte[] octets = Encoding.UTF8.GetBytes(corps);
                requete.Method = "POST";
                requete.ContentType = "application/json";
                requete.ContentLength = octets.Length;
                using (Stream flux = requete.GetRequestStream()) flux.Write(octets, 0, octets.Length);
            }
            try
            {
                using (WebResponse reponse = requete.GetResponse())
                using (var lecteur = new StreamReader(reponse.GetResponseStream(), Encoding.UTF8))
                    return lecteur.ReadToEnd();
            }
            catch (WebException e)
            {
                var http = e.Response as HttpWebResponse;          // 401 : clé refusée, 429 : trop de demandes, 5xx : le service
                if (http == null) throw;
                throw new InvalidOperationException("réponse " + (int)http.StatusCode + " du service");
            }
        }

        // Une valeur que l'utilisateur fournit : variable d'environnement d'abord, sinon un petit fichier des réglages.
        static string Lire(string variable, string fichier)
        {
            string v = variable == null ? null : Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrWhiteSpace(v))
            {
                string chemin = Path.Combine(Programme.Dossier, fichier);
                try { if (File.Exists(chemin)) v = File.ReadAllText(chemin); }
                catch (IOException) { }
            }
            return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
        }

        // Ce que rend un modèle n'est pas toujours la phrase nue : on enlève guillemets et sauts de ligne, on borne la longueur.
        static string Nettoyer(string p)
        {
            if (p == null) return null;
            p = p.Replace('\r', ' ').Replace('\n', ' ').Trim().Trim('"', '«', '»', '“', '”', ' ');
            while (p.Contains("  ")) p = p.Replace("  ", " ");
            if (p.Length > 110) { int coupe = p.LastIndexOf(' ', 107); p = p.Substring(0, coupe > 40 ? coupe : 107) + "…"; }
            return p.Length < 2 ? null : p;
        }

        // ---------------------------------------------------------------- phrases composées ici

        public static string Salut(bool anglais)
        {
            int h = DateTime.Now.Hour;
            if (anglais) return h < 5 ? "Still up? Me too!" : h < 12 ? "Good morning!" : h < 18 ? "Hello there!" : h < 22 ? "Good evening!" : "It's getting late…";
            return h < 5 ? "Encore debout ? Moi aussi !" : h < 12 ? "Bonjour !" : h < 18 ? "Coucou !" : h < 22 ? "Bonsoir !" : "Il se fait tard…";
        }

        static string Un(params string[] choix) { return choix[hasard.Next(choix.Length)]; }

        // Outil de contrôle (--paroles fichier) : des phrases comme il en écrirait, dans des situations variées et
        // dans les deux langues, sans toucher à son journal. La dernière ligne compte les répétitions (il n'en faut aucune).
        public static void Essai(string fichier, int combien)
        {
            var lignes = new List<string>();
            int doublons = 0;
            foreach (bool anglais in new[] { false, true })
                for (int i = 0; i < combien; i++)
                {
                    var c = new Contexte
                    {
                        Animation = i % 3 == 0 ? null : Biblio.Toutes[hasard.Next(Biblio.Toutes.Count)].Nom, Amis = i % 4, Fenetres = i % 6,
                        Animations = Biblio.Toutes.Count, Musique = i % 5 == 0, Perche = i % 7 == 0, Arrivee = i == 0,
                    };
                    string p = Integree(anglais, c);
                    if (!dites.Add(p)) doublons++;
                    lignes.Add(p);
                }
            lignes.Add("répétitions : " + doublons);
            File.WriteAllLines(fichier, lignes, Encoding.UTF8);
        }

        // Une phrase de circonstance : une entrée en matière, un propos tiré de ce qu'il sait (ce qu'il vient de
        // faire, l'heure, ses amis, la musique, les fenêtres, lui-même), parfois une chute. On retire jusqu'à en
        // trouver une qu'il n'a jamais écrite.
        static string Integree(bool anglais, Contexte c)
        {
            string phrase = null;
            for (int essai = 0; essai < 80; essai++)
            {
                phrase = anglais ? Anglaise(c) : Francaise(c);
                if (!dites.Contains(phrase)) return phrase;
            }
            return phrase + " (" + (dites.Count + 1) + ")";       // tout a été dit : on numérote plutôt que de se répéter
        }

        static string Francaise(Contexte c)
        {
            DateTime d = DateTime.Now;
            string heure = d.ToString("HH:mm"), jour = d.ToString("dddd", CultureInfo.GetCultureInfo("fr-FR"));
            int h = d.Hour, allume = Environment.TickCount / 3600000;
            string a = c.Animation;
            var propos = new List<string>();
            if (a != null)
                for (int i = 0; i < 3; i++)                      // ce qu'il vient de faire revient plus souvent que le reste
                    propos.Add(Un("Je viens de faire « " + a + " ». Pas mal, non ?", "Tu as vu mon « " + a + " » ?", "« " + a + " », c'est fait. Et maintenant ?",
                        "J'ai travaillé « " + a + " » toute la journée.", "Mon « " + a + " » progresse, tu ne trouves pas ?", "Encore un « " + a + " » de réussi !",
                        "Je souffle un peu après « " + a + " ».", "« " + a + " »… j'ai failli le rater, chut.", "Note mon « " + a + " » sur dix, pour voir.",
                        "« " + a + " », c'est ma spécialité."));
            propos.Add("Il est " + heure + ", " + (h < 6 ? "tu ne dors donc jamais ?" : h < 12 ? "le meilleur moment pour un salto." : h < 14 ? "ça sent le repas, non ?"
                : h < 18 ? "l'heure idéale pour grimper sur une fenêtre." : h < 22 ? "la soirée commence bien." : "tu ne devrais pas aller te coucher ?"));
            propos.Add(Un("Déjà " + heure + " ? Le temps file, sur cet écran.", "Bon " + jour + " à toi !", "Le " + jour + ", c'est mon jour préféré pour danser.", "Encore un " + jour + " passé sur ton bureau."));
            propos.Add(c.Amis == 0
                ? Un("Je suis tout seul sur cet écran. Tu m'ajoutes des amis ?", "Un stickman tout seul, c'est un stickman qui s'ennuie.", "Personne avec qui faire un check, aujourd'hui.")
                : Un("On est " + (c.Amis + 1) + " sur ton écran aujourd'hui.", "Mes " + (c.Amis == 1 ? "copain fait" : c.Amis + " copains font") + " n'importe quoi, regarde.",
                    "À " + (c.Amis + 1) + ", on ferait une belle chorégraphie.", "Les autres me suivent partout, c'est moi le chef."));
            if (c.Musique) propos.Add(Un("J'adore ce morceau !", "Monte le son, je danse !", "Cette musique me donne des fourmis dans les jambes.", "Si tu changes de musique, je change de danse."));
            if (c.Perche) propos.Add(Un("Je suis perché sur ta fenêtre, la vue est belle.", "D'ici, je vois tout ton bureau.", "Ne ferme pas cette fenêtre, je suis dessus !"));
            if (c.Fenetres > 1) propos.Add(Un("Tu as " + c.Fenetres + " fenêtres ouvertes : autant de perchoirs pour moi.", c.Fenetres + " fenêtres… je les escaladerai toutes.", "Avec " + c.Fenetres + " fenêtres, j'ai de quoi grimper."));
            if (allume >= 2) propos.Add(Un("Ton PC est allumé depuis " + allume + " heures. Moi, je ne fatigue jamais.", allume + " heures que cet écran est allumé, et je suis toujours en forme."));
            propos.Add(Un("Je suis un bonhomme-bâton, et je vis sur ton bureau.", "Je connais " + c.Animations + " animations, et j'en apprends encore.", "Je suis orange, et j'en suis fier.",
                "Mon métier : habiter ton écran.", "Je ne suis qu'un trait, mais j'ai du style.", "Pas de visage, mais plein d'idées.", "Je garde ton bureau jour et nuit.",
                "Un jour, j'aurai mon propre dessin animé.", "J'écris avec mon doigt, et sans faute.", "Marcher, grimper, danser : voilà mes journées.",
                "Je fais des saltos pendant que tu travailles.", "Je m'entraîne tous les jours pour te faire sourire.", "Entre deux animations, je veille sur ton écran.",
                "Quand tu ne regardes pas, je répète mes danses.", "Je tombe souvent, mais je me relève toujours."));
            propos.Add(Un("N'oublie pas de boire de l'eau.", "Lève-toi et étire-toi un peu.", "Tu travailles, ou tu fais semblant ?", "On fait une pause ?", "Courage, tu vas y arriver !",
                "Continue, tu gères.", "Tu me prêtes ta souris ?", "Promis, je n'ai rien cassé.", "Ne me ferme pas, je suis gentil !", "Ta barre des tâches fait un bon trottoir.",
                "J'ai compté tes icônes. Deux fois.", "C'est bientôt l'heure du goûter ?"));
            string phrase = (c.Arrivee ? Salut(false) : Un("Bonjour !", "Coucou !", "Salut toi !", "Hé !", "Psst !", "Yo !", "C'est encore moi !", "Dis donc !", "Tiens, tiens…", "Oh !", "Alors ?", "Au fait…", "Tu sais quoi ?", "Bon."))
                + " " + propos[hasard.Next(propos.Count)];
            if (phrase.Length < 70 && hasard.Next(2) == 0)
                phrase += " " + Un("Voilà.", "C'est tout.", "À plus !", "Je retourne m'entraîner.", "Bon, j'y retourne.", "Tu peux reprendre.", "Je te laisse.", "Hop !", "Fin du message.", "Signé : l'orange.");
            return phrase;
        }

        static string Anglaise(Contexte c)
        {
            DateTime d = DateTime.Now;
            string heure = d.ToString("HH:mm"), jour = d.ToString("dddd", CultureInfo.GetCultureInfo("en-US"));
            int h = d.Hour, allume = Environment.TickCount / 3600000;
            string a = c.Animation == null ? null : Langue.EnAnglais(c.Animation);
            var propos = new List<string>();
            if (a != null)
                for (int i = 0; i < 3; i++)
                    propos.Add(Un("I just did \"" + a + "\". Not bad, eh?", "Did you see my \"" + a + "\"?", "\"" + a + "\": done. What next?",
                        "I've been practising \"" + a + "\" all day.", "My \"" + a + "\" is getting better, don't you think?", "Another \"" + a + "\" nailed!",
                        "Catching my breath after \"" + a + "\".", "\"" + a + "\"… I nearly messed it up, shh.", "Rate my \"" + a + "\" out of ten.",
                        "\"" + a + "\" is my speciality."));
            propos.Add("It's " + heure + ", " + (h < 6 ? "do you ever sleep?" : h < 12 ? "the best time for a backflip." : h < 14 ? "smells like lunch, doesn't it?"
                : h < 18 ? "the perfect time to climb a window." : h < 22 ? "the evening is off to a good start." : "shouldn't you be in bed?"));
            propos.Add(Un(heure + " already? Time flies on this screen.", "Have a great " + jour + "!", jour + " is my favourite day for dancing.", "Another " + jour + " spent on your desktop."));
            propos.Add(c.Amis == 0
                ? Un("I'm all alone on this screen. Will you add some friends?", "A lonely stick figure is a bored stick figure.", "Nobody to fist-bump today.")
                : Un("There are " + (c.Amis + 1) + " of us on your screen today.", "My " + (c.Amis == 1 ? "friend is" : c.Amis + " friends are") + " up to no good, look.",
                    "With " + (c.Amis + 1) + " of us we could do a nice routine.", "The others follow me everywhere: I'm the boss."));
            if (c.Musique) propos.Add(Un("I love this track!", "Turn it up, I'm dancing!", "This music gives me itchy feet.", "Change the music and I'll change the dance."));
            if (c.Perche) propos.Add(Un("I'm perched on your window, lovely view.", "From up here I can see your whole desktop.", "Don't close this window, I'm on it!"));
            if (c.Fenetres > 1) propos.Add(Un("You have " + c.Fenetres + " windows open: that many perches for me.", c.Fenetres + " windows… I'll climb every one.", "With " + c.Fenetres + " windows I have plenty to climb."));
            if (allume >= 2) propos.Add(Un("Your PC has been on for " + allume + " hours. I never get tired.", allume + " hours this screen has been on, and I'm still fresh."));
            propos.Add(Un("I'm a stick figure, and I live on your desktop.", "I know " + c.Animations + " animations, and I'm still learning.", "I'm orange, and proud of it.",
                "My job: living on your screen.", "I'm only a line, but I have style.", "No face, but plenty of ideas.", "I guard your desktop day and night.",
                "One day I'll have my own cartoon.", "I write with my finger, with no typos.", "Walking, climbing, dancing: that's my day.",
                "I do backflips while you work.", "I practise every day to make you smile.", "Between two animations, I watch over your screen.",
                "When you're not looking, I rehearse my dances.", "I fall a lot, but I always get back up."));
            propos.Add(Un("Don't forget to drink some water.", "Stand up and stretch a little.", "Working, or pretending to?", "Shall we take a break?", "Keep going, you've got this!",
                "You're doing great.", "Can I borrow your mouse?", "I promise I broke nothing.", "Don't close me, I'm nice!", "Your taskbar makes a fine pavement.",
                "I counted your icons. Twice.", "Is it snack time yet?"));
            string phrase = (c.Arrivee ? Salut(true) : Un("Hello!", "Hi there!", "Hey you!", "Hey!", "Psst!", "Yo!", "Me again!", "I say!", "Well, well…", "Oh!", "So?", "By the way…", "Guess what?", "Right."))
                + " " + propos[hasard.Next(propos.Count)];
            if (phrase.Length < 70 && hasard.Next(2) == 0)
                phrase += " " + Un("There.", "That's all.", "See you!", "Back to practice.", "Right, back to it.", "Carry on.", "I'll leave you to it.", "Hop!", "End of message.", "Signed: the orange one.");
            return phrase;
        }
    }
}