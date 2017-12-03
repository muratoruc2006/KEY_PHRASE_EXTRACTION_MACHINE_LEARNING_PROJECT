using net.zemberek.erisim;
using net.zemberek.tr.yapi;
using net.zemberek.yapi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace KeyPhraseApp
{
    public partial class fmKeyPhrase : Form
    {
        struct word
        {
            public int sent_number;
            public int number;
            public string root_word;
            public string real_word;
            public string tag;
        };

        struct tuple
        {
            public string kp;
            public double tfidf;
        };        

        struct tuple_2
        {
            public string kp;
            public int docfreq;
            public int termfreq;
        };

        struct tuple_3
        {
            public string kp;
            public double tfidf;
            public double firstocc;
            public double rellen;
            public bool value;
        };

        struct tuple_4
        {
            public string kp;
            public double tfidf;
            public double firstocc;
        };

        struct tuple_5
        {
            public string kp;
            public double tfidf;
            public double firstocc;
            public double rellength;
        };
        
        private List<string> abbreviations = new List<string>();
        private List<string> stoplist = new List<string>();

        private List<string> KP_Candidate = new List<string>();
        private List<string> KP_Candidate_New = new List<string>();
        private List<string> KP_Candidate_Last = new List<string>();
        private List<string> KP_Candidate_Last_ = new List<string>();

        private List<string>  Real_KPs = new List<string>();
        private List<string> Final_KPs = new List<string>();
        private List<tuple_2> KPDocFreq = new List<tuple_2>();
        private List<tuple>   KPTF_IDF = new List<tuple>();
        private List<tuple_4> KPFirstOcc = new List<tuple_4>();
        private List<tuple_5> KPRelLength = new List<tuple_5>();
        private List<tuple_3> KPClass_Value = new List<tuple_3>();
        private List<tuple> KPFinal_Puan = new List<tuple>();

        private List<word> list_Cumle_Isim = new List<word>();
        private List<word> list_Cumle_KP = new List<word>();
 
        public string[] words;
        public string[] tip;
        public char[] delimiterChars = { ' ', ',', '.', ':', '\t', '\n' };
        
        public ArrayList Cumleler;
        public string alltext;
        public int dosya=0;
        //tf_idf ortalama,varyans,standart sapma global değişkenleri.
        public double mean_true_tfidf = 0.0;
        public double mean_false_tfidf = 0.0;
        public double variance_true_tfidf = 0.0;
        public double variance_false_tfidf = 0.0;
        public double standart_deviation_true_tfidf = 0.0;
        public double standart_deviation_false_tfidf = 0.0;
        //kp position ortalama,varyans,standart sapma global değişkenleri.
        public double mean_true_kppos = 0.0;
        public double mean_false_kppos = 0.0;
        public double variance_true_kppos = 0.0;
        public double variance_false_kppos = 0.0;
        public double standart_deviation_true_kppos = 0.0;
        public double standart_deviation_false_kppos = 0.0;
        //relative length ortalama,varyans,standart sapma global değişkenleri.
        public double mean_true_rel = 0.0;
        public double mean_false_rel = 0.0;
        public double variance_true_rel = 0.0;
        public double variance_false_rel = 0.0;
        public double standart_deviation_true_rel = 0.0;
        public double standart_deviation_false_rel = 0.0;
        //yes ve no sayıları.
        int count_true_all = 0;
        int count_false_all = 0;

        class ListByTermByDescendingOrder : IComparer<tuple>
        {
            public int Compare(tuple x, tuple y)
            {
                if (x.tfidf < y.tfidf) return 1;
                else if (x.tfidf > y.tfidf) return -1;
                else return 0;
            }
        }

        public fmKeyPhrase()
        {
            InitializeComponent();
        }

        private void fmKeyPhrase_Load(object sender, EventArgs e)
        {
            tcKeyPhrase.SelectedTab = tpPOSTagger;
            
            //stoplist i oluşturuyoruz...
            const string f = "stoplist.txt";
            stoplist.Clear();
            using (StreamReader r = new StreamReader(f, Encoding.GetEncoding(1254)))
            {
                string line;
                while ((line = r.ReadLine()) != null)
                {
                    stoplist.Add(line);
                }
            }

            tbMeanAll.Text = "0,225499198910808";
            tbVarianceAll.Text = "0,056618267658991";
            tbStandardDevAll.Text = "0,143887660662142";
            tbMeanAll_.Text = "0,034462123894954";
            tbVarianceAll_.Text = "0,00265483463205546";
            tbStandardDevAll_.Text = "0,0493710121560926";

            tbMeanAll_KPPos.Text = "0,248019672068224";
            tbVarianceAll_KPPos.Text = "0,0586469545039266";
            tbStandardDevAll_KPPos.Text = "0,151009259578354";
            tbMeanAll_KPPos_.Text = "0,0469395624008983";
            tbVarianceAll_KPPos_.Text = "0,00473645740152245";
            tbStandardDevAll_KPPos_.Text = "0,0565849271290234";

            tbMeanAll_Rel.Text = "0,231931445909093";
            tbVarianceAll_Rel.Text = "0,0567837628647788";
            tbStandardDevAll_Rel.Text = "0,145921715760878";
            tbMeanAll_Rel_.Text = "0,0456432594072062";
            tbVarianceAll_Rel_.Text = "0,00345994311725022";
            tbStandardDevAll_Rel_.Text = "0,0538574040393178";

            tbLaPlaceMetric_10.Text = "0,291369047619048";
            tbLaPlaceMetric_5.Text = "0,267559523809524";

            tbAccuracy_10.Text = "0,175";
            tbAccuracy_5.Text = "0,14";

            tbTrue.Text = "57";
            tbFalse.Text = "57920";
        }

        private ArrayList IDontCareHowItEndsParser(string sTextToParse)
        {
            string sTemp = sTextToParse;
            sTemp = sTemp.Replace(Environment.NewLine, " ");
            sTemp = sTemp.Replace("\n", " ");

            //Rakamları remove ediyoruz...
            for (int i = 0; i < 10; i++)
            {
                sTemp = sTemp.Replace(i.ToString(),string.Empty);
            }

            sTemp = sTemp.Trim();
            //string[] splitSentences1 = Regex.Split(sTemp, @"(?<=['""A-Za-z0-9][\.\!\?])\s+(?=[A-Z])");

            //abbreviations txt file ını listeye dönüştürüyoruz...
            const string f = "abbreviations.txt";

            using (StreamReader r = new StreamReader(f, Encoding.GetEncoding(1254)))
            {
                string line;
                while ((line = r.ReadLine()) != null)
                {
                    abbreviations.Add(line);
                }
            }

            foreach (string word in abbreviations)
            {
                if (sTemp.Contains(word))
                {
                    sTemp = sTemp.Replace(word, word.Substring(0, (word.Length) - 1) + "");
                }
            }
            
            //Punctuation Mark ları remove ediyoruz...
            sTemp = sTemp.Replace("\"", string.Empty);
            sTemp = sTemp.Replace("\'", string.Empty);
            sTemp = sTemp.Replace("(", string.Empty);
            sTemp = sTemp.Replace(")", string.Empty);
            sTemp = sTemp.Replace("“", string.Empty);
            sTemp = sTemp.Replace("”", string.Empty);
            sTemp = sTemp.Replace(";", string.Empty);
            sTemp = sTemp.Replace("*", string.Empty);
            sTemp = sTemp.Replace("[", string.Empty);
            sTemp = sTemp.Replace("]", string.Empty);
            sTemp = sTemp.Replace("“", string.Empty);
            sTemp = sTemp.Replace("”", string.Empty);
            sTemp = sTemp.Replace("’", string.Empty);
            sTemp = sTemp.Replace("‘", string.Empty);
            sTemp = sTemp.Replace("-", string.Empty);            

            // split the string using sentence terminations
            char[] arrSplitChars = { '.', '?', '!' };  // things that end a sentence
            ArrayList al = new ArrayList();

            //do the split
            string[] splitSentences = sTemp.Split(arrSplitChars, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < splitSentences.Length; i++)
            {
                splitSentences[i] = splitSentences[i].ToString().Trim();
                if ((splitSentences[i].ToString() != ""))
                {
                    al.Add(splitSentences[i].ToString());
                }
            }

            return al;
        }

        public void TestStringSplit()
        {
            int kelime_sayi=0;
            int stoplistdeVarMi;
            list_Cumle_Isim.Clear();
            lbNouns.Items.Clear();
            lbWords.Items.Clear();

            Zemberek zemberek = new Zemberek(new TurkiyeTurkcesi());

            rtbCozumleme.Text = "";
            stoplistdeVarMi = 0;

            //pos tagging döngüsü.
            for (int i = 0; i < Cumleler.Count; i++)
            {
                words = (Cumleler[i].ToString()).Split(delimiterChars);
                kelime_sayi = 0;

                foreach (string s in words)
                {
                    kelime_sayi += 1;

                    Kelime[] cozumler = zemberek.kelimeCozumle(s);
                    bool t = zemberek.kelimeDenetle(s);

                    lbWords.Items.Add(s);
                    
                    //kelimeyi ZEMBEREK bulamadıysa kelimeyi olduğu gibi listeye ekle, ve tipine XXX ver...
                    if (t == false)
                    {
                        list_Cumle_Isim.Add(new word()
                        {
                            sent_number = i + 1,
                            number = kelime_sayi,
                            root_word = s,
                            real_word = s,
                            tag = "XXX"
                        });
                        lbNouns.Items.Add((i + 1).ToString() + " " +
                                                kelime_sayi.ToString() + " " +
                                                s + " " +
                                                "XXX");
                        //break;
                    }

                    stoplistdeVarMi = 0;

                    //kelime stop word ve en az iki harfli ise; kelimeyi olduğu gibi ekle ve tipine STOP ver...
                    if (s.Length >= 2)
                    {
                        foreach (string word in stoplist)
                        {
                            if (s == word)
                            {
                                stoplistdeVarMi = 1;
                                list_Cumle_Isim.Add(new word()
                                {
                                    sent_number = i + 1,
                                    number = kelime_sayi,
                                    root_word = s,
                                    real_word = s,
                                    tag = "STOP"
                                });
                                lbNouns.Items.Add((i + 1).ToString() + " " +
                                                    kelime_sayi.ToString() + " " +
                                                    s +
                                                    " STOP");
                                break;
                            }
                        }
                    }

                    foreach (net.zemberek.yapi.Kelime kelime_ in cozumler)
                    {
                        //zemberek butonu açıldığı zaman onun altına konulacak kod parçası...
                        //if ((t == true) && (kelime_.icerik().Length > 2))
                        //{
                        //    rtbCozumleme.Text = rtbCozumleme.Text + kelime_.ToString() + "\n";
                        //}

                        //stop word değilse ve kelimenin kökü en az 3 harfli ise o zaman kelime havuzuna at!
                        if ( (stoplistdeVarMi == 0) && (kelime_.kok().icerik().Length>2) ) 
                        {
                            list_Cumle_Isim.Add(new word() { sent_number = i + 1,
                                                                number = kelime_sayi,
                                                                root_word = kelime_.kok().icerik(),
                                                                real_word = s,
                                                                tag = kelime_.kok().tip().ToString() });
                            lbNouns.Items.Add( (i + 1).ToString() + " " +
                                                    kelime_sayi.ToString() + " " +               
                                                    kelime_.kok().icerik() + " "+
                                                    kelime_.kok().tip().ToString());
                            break;
                        }
                    }                
                }                              
            }
            
        }

        private void NounPhraseSplit()
        {
            int sayi = 0;
            string tag="";
            string xxx="";
            list_Cumle_KP.Clear();
            KP_Candidate.Clear();

            //sadece ISIM,SIFAT,OZEL,STOP tipli kelimeleri bir havuza al.
            foreach (word w in list_Cumle_Isim)
            {
                if ((w.tag == "ISIM") || (w.tag == "SIFAT") || (w.tag == "OZEL") || (w.tag == "STOP"))
                {
                    list_Cumle_KP.Add(new word { sent_number=w.sent_number,
                                                    number=w.number,
                                                    root_word = w.root_word,
                                                    real_word=w.real_word,
                                                    tag=w.tag});                     
                                     
                }
            }

            //ISIM,OZEL,SIFAT tipli kelimelerle başlayan ve ISIM veya OZEL tipli kelimelerle biten key phrase leri oluşturur.
            //Başlangıç ve bitiş kelimeleri arasında stop word olabilir.
            string root = "";
            foreach (word w in list_Cumle_KP)
            {
                //sayi ile w.number birbirine eşit değilse yeni bir anahtar kelimeye geçti demektir. o zaman bu eşitsizlik bize anahtar kelimeyi tespit etmemize yardımcı oluyor.
                //tabi anahtar kelimenin bitiş şartı isim veya özel isim olmak zorunda.
                if ((sayi != w.number - 1) && (tag == "ISIM" || tag == "OZEL") )
                {
                    if (xxx != "")
                    {
                        //tek kelime olduğunu aralarda " "(boşluk) olup olmadığına göre karar veriyoruz.
                        //anahtar kelime tek kelime ise kelimenin kök halini ekle.
                        if (xxx.Trim().ToLower().Contains(" ")==false)
                        {
                            KP_Candidate.Add(root);
                            lbNounPhrases.Items.Add(root);
                            xxx = "";
                        }
                        //anahtar kelime birden fazla kelimeden oluşuyorsa o zaman kelimelerin orijinal halini ekle...
                        else
                        {
                            KP_Candidate.Add(xxx.ToString());
                            lbNounPhrases.Items.Add(xxx.ToString());
                            xxx = "";
                        }
                    }                    

                    //başlama kelimesi şartları: isim, özel isim veya sıfat olmak zorunda.
                    //stop word ile başlamasın diye konuldu.
                    if ((w.tag == "ISIM") || (w.tag == "SIFAT") || (w.tag == "OZEL"))
                        xxx=xxx+w.real_word+" ";
                }
                else
                {
                    xxx = xxx + w.real_word+" ";
                }                
                sayi=w.number;
                tag = w.tag;
                //tek kelime olan anahtar kelimeleri eklemek için kökü root diye bir değişkende tutuyoruz.
                root = "";
                root = w.root_word;
            }
            //foreach (word w in list_Cumle_Isim)
            //{
            //    if (w.tag == "XXX" && w.real_word.Trim() != "" && w.real_word.Trim().Length>2)
            //        KP_Candidate.Add(w.real_word.Trim());
            //}
        }

        private void NounPhraseGenerate()
        {
            int stoplistdeVarMi = 0;
            int kelime_sayi = 0;
            string _tip = "";
            KP_Candidate_New.Clear();
            KP_Candidate_Last_.Clear();
            lbCandidateKP.Items.Clear();

            Zemberek zemberek = new Zemberek(new TurkiyeTurkcesi());

            //her bir keyphrase için bütün olasılıklı key phraseleri üret!
            foreach (string phrase in KP_Candidate)
            {
  
                words = (phrase.ToString().Trim()).Split(delimiterChars);

                kelime_sayi = words.Count();

                //anahtar kelime birden fazla kelimeden olşuyorsa bu ife gir!
                if (kelime_sayi > 1)
                {
                    _tip = "";

                    //keyphrase'in döngüsü(tipleri toplamak için)
                    for (int i = 0; i < kelime_sayi; i++)
                    {
                        stoplistdeVarMi = 0;
                        //stop word ise;
                        foreach (string word in stoplist)
                        {
                            if (words[i].Trim().ToString() == word)
                            {
                                stoplistdeVarMi = 1;
                                _tip = _tip + "STOP" + " ";
                            }
                        }
                        //stop word değil ise;(isim,sıfat veya özel isim ise;)
                        if (stoplistdeVarMi == 0)
                        {
                            Kelime[] cozumler = zemberek.kelimeCozumle(words[i].Trim().ToString());
                            _tip = _tip + cozumler[0].kok().tip().ToString() + " ";
                        }
                    }
                    _tip = _tip.Trim();

                    //burada key phrase'in kelimelerinin tiplerini "tip" adlı bir array'de tutuyoruz.
                    tip = _tip.ToString().Split(delimiterChars);
                    string new_KP = "";

                    //anahtar kelimenin varyasyonlarını üret!
                    for (int i = 0; i < kelime_sayi; i++)
                    {
                        //eğer stop word ise dögüye bir sonraki eleman ile devam et!
                        if (tip[i].ToString() != "STOP") new_KP = words[i].ToString();
                        else continue;

                        for (int j = i + 1; j < kelime_sayi; j++)
                        {
                            string old_KP = new_KP;

                            new_KP = new_KP + " " + words[j];

                            //bitiş kelimesi stop veya sıfat olmasın...
                            if ((old_KP.Trim() != new_KP.Trim()) && ((tip[j].ToString() != "STOP") && (tip[j].ToString() != "SIFAT")))
                            {
                                KP_Candidate_New.Add(new_KP.ToString().ToLower().Trim());
                            }
                        }

                    }
                }
                //anahtar kelime tek kelime ise direkt ekle!
                else KP_Candidate_New.Add(phrase.ToString().ToLower().Trim());
            }

            //duplicate kayıtları distinct fonksiyonu ile ele...
            KP_Candidate_Last_ = KP_Candidate_New.Distinct().ToList();          

            //KP_Candidate_Last_.Sort();

            foreach (string s in KP_Candidate_Last_)
            {
                lbCandidateKP.Items.Add(s.ToString());
            }

        }

        private void KPleriFiltrele()
        {
            string ph;
            int kelime_sayi = 0;
            KP_Candidate_Last.Clear();
            lbKP_Last.Items.Clear();

            foreach (string kp in KP_Candidate_Last_)
            {
                ph = kp.ToString().Trim();
                words = (kp.ToString()).Split(delimiterChars);
                kelime_sayi = words.Count();

                //5 kelime ve daha küçük anhtar kelimeleri işleme koy...
                if (kelime_sayi < 6)
                {
                    KP_Candidate_Last.Add(kp);
                }
            }

            //KP_Candidate_Last.Sort();

            foreach (string s in KP_Candidate_Last)
            {
                lbKP_Last.Items.Add(s.ToString());
            }
        }

        private void DosyalarıOku(int dosya_number)
        {
            string dosya;
            alltext = "";
            if (dosya_number.ToString().Length == 1) dosya = "0" + dosya_number.ToString();
            else dosya = dosya_number.ToString();

            string text = "corpora\\" + dosya + ".txt";

            using (StreamReader r = new StreamReader(text, Encoding.GetEncoding(1254)))
            {                
                alltext = r.ReadToEnd();
            }
        }

        private void KeyPhraseleriOku(int dosya_number)
        {
            string dosya;
            Real_KPs.Clear();
            if (dosya_number.ToString().Length == 1) dosya = "0" + dosya_number.ToString();
            else dosya = dosya_number.ToString();

            string text = "E:\\IT\\Courses\\MachineLearning\\KEYPHRASE\\corpora\\Gazi_Muhendislik_Dergisi\\" + dosya + "key.key";

            using (StreamReader r = new StreamReader(text, Encoding.GetEncoding(1254)))
            {
                string line;
                while ((line = r.ReadLine()) != null)
                {
                    Real_KPs.Add(line);
                }
            }
        }

        private void TestDataImplement(object sender,EventArgs e, int Kpcount)
        {
            double LaPlace = 0.0;
            double Accuracy = 0.0;
            KPFinal_Puan.Clear();
            lbKeyPhrasesAuthor.Items.Clear();
            lbKp_Extracted.Items.Clear();

            //yes ve no sayılarının atanması.
            int count_true = Int32.Parse(tbTrue.Text);
            int count_false = Int32.Parse(tbFalse.Text);
            int count = 0;
            for (int i = Int32.Parse(tbBas.Text) + 40; i <= Int32.Parse(tbBit.Text) + 40; i++)
            {
                DosyalarıOku(i);
                txtParagraphs.Text = alltext;
                dosya = i;

                btCumlelereBol_Click(sender, e);
                btKelimelereBol_Click(sender, e);
                btAdayKpUret_Click(sender, e);
                btKpFiltrele_Click(sender, e);
                btTerimFrekansBul_Click(sender, e);
                btDokumanFrekans_Click(sender, e);
                btTF_IDF_Click(sender, e);
                btFirstOccurence_Click(sender, e);
                btRelativeLength_Click(sender, e);

                //tfidf, kpposition ve relative lentgh özelliklerinin P(probability) değerlerinin bulunması.
                //Probability değerlerini normal distribution formülü ile buluyoruz.
                double P_tfidf = 0.0;
                double P_kppos = 0.0;
                double P_rellen = 0.0;

                double P_tfidf_ = 0.0;
                double P_kppos_ = 0.0;
                double P_rellen_ = 0.0;

                double P_yes = 0.0;
                double P_no = 0.0;
                double P_final = 0.0;

                KPFinal_Puan.Clear();

                foreach (tuple_5 t in KPRelLength)
                {
                    //yes parametreleri...
                    P_tfidf = 0.0;
                    P_tfidf = (1 / ((Math.Sqrt(2 * Math.PI)) * Double.Parse(tbVarianceAll.Text.ToString()))) *
                                (Math.Pow(Math.E, Math.Pow((t.tfidf - Double.Parse(tbMeanAll.Text.ToString())), 2) / (2 * Double.Parse(tbStandardDevAll.Text.ToString())) * -1));
                    P_kppos = 0.0;
                    P_kppos = (1 / ((Math.Sqrt(2 * Math.PI)) * Double.Parse(tbVarianceAll_KPPos.Text.ToString()))) *
                                (Math.Pow(Math.E, Math.Pow((t.firstocc - Double.Parse(tbMeanAll_KPPos.Text.ToString())), 2) / (2 * Double.Parse(tbStandardDevAll_KPPos.Text.ToString())) * -1));
                    P_rellen = 0.0;
                    P_rellen = (1 / ((Math.Sqrt(2 * Math.PI)) * Double.Parse(tbVarianceAll_Rel.Text.ToString()))) *
                                (Math.Pow(Math.E, Math.Pow((t.rellength - Double.Parse(tbMeanAll_Rel.Text.ToString())), 2) / (2 * Double.Parse(tbStandardDevAll_Rel.Text.ToString())) * -1));

                    P_yes = 0.0;
                    P_yes = ((double)count_true / (count_true + count_false)) * P_tfidf * P_kppos * P_rellen;

                    //no parametreleri...
                    P_tfidf_ = 0.0;
                    P_tfidf_ = (1 / ((Math.Sqrt(2 * Math.PI)) * Double.Parse(tbVarianceAll_.Text.ToString()))) *
                                (Math.Pow(Math.E, Math.Pow((t.tfidf - Double.Parse(tbMeanAll_.Text.ToString())), 2) / (2 * Double.Parse(tbStandardDevAll_.Text.ToString())) * -1));
                    P_kppos_ = 0.0;
                    P_kppos_ = (1 / ((Math.Sqrt(2 * Math.PI)) * Double.Parse(tbVarianceAll_KPPos_.Text.ToString()))) *
                                (Math.Pow(Math.E, Math.Pow((t.firstocc - Double.Parse(tbMeanAll_KPPos_.Text.ToString())), 2) / (2 * Double.Parse(tbStandardDevAll_KPPos_.Text.ToString())) * -1));
                    P_rellen_ = 0.0;
                    P_rellen_ = (1 / ((Math.Sqrt(2 * Math.PI)) * Double.Parse(tbVarianceAll_Rel_.Text.ToString()))) *
                                (Math.Pow(Math.E, Math.Pow((t.rellength - Double.Parse(tbMeanAll_Rel_.Text.ToString())), 2) / (2 * Double.Parse(tbStandardDevAll_Rel_.Text.ToString())) * -1));

                    P_no = 0.0;
                    P_no = ((double)count_false / (count_true + count_false)) * P_tfidf_ * P_kppos_ * P_rellen;

                    //son probability...
                    P_final = 0.0;
                    P_final = P_yes / (P_yes + P_no);

                    KPFinal_Puan.Add(new tuple { kp = t.kp, tfidf = P_final });
                }

                KPFinal_Puan = KPFinal_Puan.OrderByDescending(a => a.tfidf).ToList();

                KeyPhraseleriOku(i);

                int kp_sayi = 0;
                int dogru_kp = 0;
                Final_KPs.Clear();

                foreach (tuple t in KPFinal_Puan)
                {
                    kp_sayi++;
                    Final_KPs.Add(t.kp);
                    if (kp_sayi == Kpcount)
                        break;
                }

                foreach (string t in Final_KPs)
                {
                    lbKp_Extracted.Items.Add(i.ToString() + ". " + t);
                }

                lbKp_Extracted.Items.Add("");
                foreach (string s in Real_KPs)
                {
                    if (s.Trim()!="")
                    lbKeyPhrasesAuthor.Items.Add(i.ToString() + ". " + s);
                    foreach (string t in Final_KPs)
                    {
                        if (t.ToLower().Trim() == s.ToLower().Trim())
                        {
                            dogru_kp++;
                            break;
                        }
                    }
                }
                lbKeyPhrasesAuthor.Items.Add("");
                
                LaPlace = LaPlace + ((double)dogru_kp + 1) / (Real_KPs.Count + 2);
                Accuracy = Accuracy + ((double)dogru_kp) / (Real_KPs.Count);
                count++;
            }
            LaPlace = LaPlace / count;
            Accuracy = Accuracy / count;
            if (Kpcount == 10)
            {
                tbLaPlaceMetric_10.Text = LaPlace.ToString();
                tbAccuracy_10.Text = Accuracy.ToString();
            }
            else if (Kpcount == 5)
            {
                tbLaPlaceMetric_5.Text = LaPlace.ToString();
                tbAccuracy_5.Text = Accuracy.ToString();
            }
        }

        private void btDosyaAc_Click(object sender, EventArgs e)
        {
            //ilk önce eski verileri temizliyoruz.
            btTemizle_Click(sender, e);
            lbNounPhrases.Items.Clear();
            lbCandidateKP.Items.Clear();
            lbKP_Last.Items.Clear();

            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = @"C:\";
            openFileDialog1.Title = "Browse Text Files";

            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;
            openFileDialog1.DefaultExt = "txt";

            openFileDialog1.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.ReadOnlyChecked = true;
            openFileDialog1.ShowReadOnly = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                tbBrowse.Text = openFileDialog1.FileName;
            }

            if (tbBrowse.Text != "")
            {
                string file = tbBrowse.Text;
                txtParagraphs.Text = "";

                using (StreamReader r = new StreamReader(file, Encoding.GetEncoding(1254)))
                {
                    alltext = r.ReadToEnd();
                }

                //using (StreamReader r = new StreamReader(file, Encoding.GetEncoding(1254)))
                //{
                //    string line;
                //    while ((line = r.ReadLine()) != null)
                //    {
                //        txtParagraphs.Text = txtParagraphs.Text + line + "\n";
                //    }
                //}

                txtParagraphs.Text = alltext;
                rtbOzetMain.Text = alltext;
            }
            else MessageBox.Show("Herhangi bir dosya seçmediniz.");
        }

        private void btCumlelereBol_Click(object sender, EventArgs e)
        {
            lbSentences.Items.Clear();
            //Cumleler.Clear();
            Cumleler = IDontCareHowItEndsParser(txtParagraphs.Text);

            for (int i = 0; i < Cumleler.Count; i++)
            {
                lbSentences.Items.Add(Cumleler[i].ToString());
            }            
        }

        private void btKelimelereBol_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            lbWords.Items.Clear();
            lbNouns.Items.Clear();
            rtbCozumleme.Text = "";

            TestStringSplit();
            this.Cursor = Cursors.Default;
        }

        private void btZemberek_Click(object sender, EventArgs e)
        {
            Zemberek zemberek = new Zemberek(new TurkiyeTurkcesi());

            for (int i = 0; i < Cumleler.Count; i++)
            {
                words = (Cumleler[i].ToString()).Split(delimiterChars);

                foreach (string s in words)
                {

                    Kelime[] cozumler = zemberek.kelimeCozumle(s);
                    bool t = zemberek.kelimeDenetle(s);

                    foreach (net.zemberek.yapi.Kelime kelime_ in cozumler)
                    {
                        //zemberek butonu açıldığı zaman onun altına konulacak kod parçası...
                        if ((t == true) && (kelime_.icerik().Length > 2))
                        {
                            rtbCozumleme.Text = rtbCozumleme.Text + kelime_.ToString() + "\n";
                        }
                    }
                }
            }
        }

        private void btTemizle_Click(object sender, EventArgs e)
        {
            txtParagraphs.Text = "";
            lbSentences.Items.Clear();
            lbWords.Items.Clear();
            lbNouns.Items.Clear();
            rtbCozumleme.Text = "";
        }
        
        private void btAdayKpUret_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            lbNounPhrases.Items.Clear();

            NounPhraseSplit();
            NounPhraseGenerate();
            this.Cursor = Cursors.Default;
        }

        private void btKpFiltrele_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            KPleriFiltrele();
            this.Cursor = Cursors.Default;
        }
        
        private void btTerimFrekansBul_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;

            dgvTermFreq.Rows.Clear();            
            int count = 0;

            //her bir key phrase için terim frekansını buluyoruz.
            foreach (object item in lbKP_Last.Items)
            {
                count = 0;

                //ilk candidate listesi içerisinde döngüye sokarak kaç tane geçtiğini buluyoruz.
                foreach (string kp in KP_Candidate)
                {
                    if (kp.ToString().Trim().ToLower().Contains(item.ToString().Trim().ToLower()))
                    {
                        count++;
                    }
                }
                string[] row1 = new string[] { item.ToString().Trim().ToLower(), count.ToString() };

                dgvTermFreq.Rows.Add(row1[0], Int32.Parse(row1[1]));
            }
            //oluşan terim frekanslarını çoktan aza doğru sıralıyoruz.
            dgvTermFreq.Sort(dgvTermFreq.Columns[1], ListSortDirection.Descending);

            this.Cursor = Cursors.Default;
        }

        private void btDokumanFrekans_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            dgvDocFreq.Rows.Clear();
            KPDocFreq.Clear();

            //oluşan bütün terim frekanslarındaki anahtar kelimeler için doküman frekansını buluyoruz.
            for (int j = 0; j < dgvTermFreq.Rows.Count-1; j++)
            {
                string term = dgvTermFreq.Rows[j].Cells[0].Value.ToString();
                int count = 0;
                //anahtar kelimelerini teker teker elimizdeki 60 dokümanda geçip geçmediğine bakıyoruz. 
                for (int i = 1; i < 61; i++)
                {
                    DosyalarıOku(i);

                    if (alltext.Trim().ToLower().Contains(term.Trim().ToLower()))
                        count++;
                }

                KPDocFreq.Add(new tuple_2 { kp = term, docfreq = count, termfreq = Int32.Parse(dgvTermFreq.Rows[j].Cells[1].Value.ToString())  });

                string[] row1 = new string[] { term.ToString().Trim().ToLower(), count.ToString() };

                dgvDocFreq.Rows.Add(row1[0], Int32.Parse(row1[1]));
            }
            //oluşan doküman frekanslarını çoktan aza doğru sıralıyoruz.
            dgvDocFreq.Sort(dgvDocFreq.Columns[1], ListSortDirection.Descending);
            alltext = "";

            Cursor.Current = Cursors.Default;   
         }      

        private void btTF_IDF_Click(object sender, EventArgs e)
        {
            dgvTF_IDF.Rows.Clear();
            KPTF_IDF.Clear();

            double TF_IDF = 0.0; 
            int size_D = 0;
            words = (txtParagraphs.Text.ToString()).Split(delimiterChars);
            size_D = words.Count();

            foreach (tuple_2 tuple in KPDocFreq)
            {
                TF_IDF = 0;
                TF_IDF = ((tuple.termfreq) / (double)(size_D)) *
                                ((double)Math.Log((60 + 60)/*Toplam doküman sayısı*/ / ((double)tuple.docfreq + 1), 2.0));
                KPTF_IDF.Add(new tuple { kp = tuple.kp, tfidf = TF_IDF });                
            }
            KPTF_IDF = KPTF_IDF.OrderByDescending(a => a.tfidf).ToList();
            //normalizasyon için maksimum tf_idf değerini alıyoruz.
            double max_tfidf = KPTF_IDF.First().tfidf;

            //normalizayon işlemi başlıyor.
            KPTF_IDF.Clear();
            foreach (tuple_2 tuple in KPDocFreq)
            {
                TF_IDF = 0;

                TF_IDF = ((tuple.termfreq) / (double)(size_D)) *
                            ((double)Math.Log((60+60)/*Toplam doküman sayısı*/ / ((double)tuple.docfreq+1), 2.0));

                KPTF_IDF.Add(new tuple { kp = tuple.kp, tfidf = TF_IDF / max_tfidf });
            }
            KPTF_IDF = KPTF_IDF.OrderByDescending(a => a.tfidf).ToList();

            foreach (tuple t in KPTF_IDF)
            {
                string[] row1 = new string[] { t.kp.ToString().Trim().ToLower(), t.tfidf.ToString() };
                dgvTF_IDF.Rows.Add(row1[0], row1[1]);
            }
        }

        private void btFirstOccurence_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;

            dgvFirstOccurence.Rows.Clear();
            KPFirstOcc.Clear();
            int count_fo = 0;

            if (alltext != "") words = alltext.Split(delimiterChars);
            else words = txtParagraphs.Text.Split(delimiterChars);

            int all_words_count = 0;
            all_words_count = words.Count();

            //her bir key phrase için first occurence değerini buluyoruz.
            foreach (tuple t in KPTF_IDF)
            {
                count_fo = 0;

                foreach (string s in KP_Candidate_Last)
                {
                    count_fo++;
                    if (t.kp == s)
                    {
                        KPFirstOcc.Add(new tuple_4 { kp = t.kp, tfidf = t.tfidf, firstocc = 1 - (count_fo / (double)KP_Candidate_Last.Count()) });
                        break;
                    }
                }
            }

            //oluşan first occurence değerlerini çoktan aza doğru sıralıyoruz.
            KPFirstOcc = KPFirstOcc.OrderByDescending(a => a.firstocc).ToList();

            foreach (tuple_4 t in KPFirstOcc)
            {
                string[] row1 = new string[] { t.kp.ToString().Trim().ToLower(), t.firstocc.ToString() };
                dgvFirstOccurence.Rows.Add(row1[0], row1[1]);
            }

            this.Cursor = Cursors.Default;
        }

        private void btRelativeLength_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;

            dgvRelativeLength.Rows.Clear();
            KPRelLength.Clear();
            int len = 0;
            //her bir key phrase için uzunlukları buluyoruz.
            foreach (tuple_4 t in KPFirstOcc)
            {
                len = 0;
                len = t.kp.Length;
                KPRelLength.Add(new tuple_5 { kp = t.kp, tfidf = t.tfidf, firstocc = t.firstocc, rellength = len });
            }
            KPRelLength = KPRelLength.OrderByDescending(a => a.rellength).ToList();
            //maksimum length değerini alıyoruz.
            double max_rellen = KPRelLength.First().rellength;

            //her bir key phrase için relative length değerlerini buluyoruz.
            KPRelLength.Clear();
            double rellen = 0.0;
            foreach (tuple_4 t in KPFirstOcc)
            {
                rellen = 0.0;
                rellen = (t.kp.Length) / (double)max_rellen;
                KPRelLength.Add(new tuple_5 { kp = t.kp, tfidf = t.tfidf, firstocc = t.firstocc, rellength = rellen });
            }
            KPRelLength = KPRelLength.OrderByDescending(a => a.rellength).ToList();

            foreach (tuple_5 t in KPRelLength)
            {
                string[] row1 = new string[] { t.kp.ToString().Trim().ToLower(), t.rellength.ToString() };
                dgvRelativeLength.Rows.Add(row1[0], row1[1]);
            }

            this.Cursor = Cursors.Default;
        }

        private void btClassValueBul_Click(object sender, EventArgs e)
        {
            if (dosya == 0) dosya = 1;
            dgvTrainingStage.Rows.Clear();
            KPClass_Value.Clear();

            bool class_value = false;
            dosya = Int32.Parse(tbBrowse.Text.Substring(tbBrowse.Text.Length - 6, 2));
            KeyPhraseleriOku(dosya);
            foreach (tuple_5 t in KPRelLength)
            {
                class_value = false;
                foreach (string s in Real_KPs)
                {
                    if (s == t.kp) class_value = true;                    
                }
                KPClass_Value.Add(new tuple_3 { kp = t.kp, tfidf = t.tfidf, firstocc = t.firstocc, rellen = t.rellength, value = class_value });
                string[] row1 = new string[] { t.kp.ToString().Trim().ToLower(), class_value.ToString() };
                dgvTrainingStage.Rows.Add(row1[0], row1[1]);
                if (class_value == true) count_true_all++;
                else if (class_value==false) count_false_all++;
            }
        }

        private void btMeanVarianceBul_Click_1(object sender, EventArgs e)
        {
            
            double toplam_true = 0.0;
            double toplam_false= 0.0;
            int count_true = 0;
            int count_false = 0;
            

            #region TF_IDF
            mean_true_tfidf = 0.0;
            mean_false_tfidf = 0.0;
            variance_true_tfidf = 0.0;
            variance_false_tfidf = 0.0;
            standart_deviation_true_tfidf = 0.0;
            standart_deviation_false_tfidf = 0.0;

            //ortalamanın (mean) bulunması. (TF_IDF)
            foreach (tuple_3 t in KPClass_Value)
            {
                if (t.value == true)
                {
                    toplam_true = toplam_true + t.tfidf;
                    count_true++;
                }
                else if (t.value == false)
                {
                    toplam_false = toplam_false + t.tfidf;
                    count_false++;
                }
            }
            mean_true_tfidf = toplam_true / ((double)count_true+0.1);
            mean_false_tfidf = toplam_false / (double)count_false;
            tbMean.Text = mean_true_tfidf.ToString();
            tbMean_.Text = mean_false_tfidf.ToString();

            //varyansın bulunması. (TF_IDF)
            double fark_true = 0.0;
            double fark_false = 0.0;

            double toplam_fark_true = 0.0;
            double toplam_fark_false = 0.0;

            foreach (tuple_3 t in KPClass_Value)
            {
                fark_true = 0.0;
                fark_false = 0.0;

                if (t.value == true) fark_true = Math.Pow((Math.Abs(t.tfidf - mean_true_tfidf)),2);
                if (t.value == false) fark_false = Math.Pow((Math.Abs(t.tfidf - mean_false_tfidf)),2);

                toplam_fark_true = toplam_fark_true + fark_true;
                toplam_fark_false = toplam_fark_false + fark_false;
            }

            variance_true_tfidf = toplam_fark_true / ((double)(count_true - 1)+0.1);//+0.1 smoothing için konuldu.
            variance_false_tfidf = toplam_fark_false / (double)(count_false - 1);

            tbVariance.Text = variance_true_tfidf.ToString();
            tbVariance_.Text = variance_false_tfidf.ToString();

            //standart sapmanın bulunması. (TF_IDF)
            standart_deviation_true_tfidf = Math.Sqrt(variance_true_tfidf);
            standart_deviation_false_tfidf = Math.Sqrt(variance_false_tfidf);

            tbStandardDev.Text = standart_deviation_true_tfidf.ToString();
            tbStandardDev_.Text = standart_deviation_false_tfidf.ToString();

            #endregion

            #region KP_Position
            mean_true_kppos = 0.0;
            mean_false_kppos = 0.0;
            variance_true_kppos = 0.0;
            variance_false_kppos = 0.0;
            standart_deviation_true_kppos = 0.0;
            standart_deviation_false_kppos = 0.0;
            toplam_true = 0;
            toplam_false = 0;

            //ortalamanın (mean) bulunması. (First Occurence)            
            foreach (tuple_3 t in KPClass_Value)
            {
                if (t.value == true)
                {
                    toplam_true = toplam_true + t.firstocc;
                    //count_true++;
                }
                else if (t.value == false)
                {
                    toplam_false = toplam_false + t.firstocc;
                    //count_false++;
                }
            }
            mean_true_kppos = toplam_true / ((double)count_true+0.1);
            mean_false_kppos = toplam_false / (double)count_false;
            tbMean_FO.Text = mean_true_kppos.ToString();
            tbMean_FO_.Text = mean_false_kppos.ToString();

            //varyansın bulunması. (First Occurence)
            fark_true = 0.0;
            fark_false = 0.0;

            toplam_fark_true = 0.0;
            toplam_fark_false = 0.0;

            foreach (tuple_3 t in KPClass_Value)
            {
                fark_true = 0.0;
                fark_false = 0.0;

                if (t.value == true) fark_true = Math.Abs(t.firstocc - mean_true_kppos);
                if (t.value == false) fark_false = Math.Abs(t.firstocc - mean_false_kppos);

                fark_true = fark_true * fark_true;
                fark_false = fark_false * fark_false;

                toplam_fark_true = toplam_fark_true + fark_true;
                toplam_fark_false = toplam_fark_false + fark_false;
            }

            variance_true_kppos = toplam_fark_true / ((double)(count_true - 1) + 0.1);//+0.1 smoothing için konuldu.
            variance_false_kppos = toplam_fark_false / (double)(count_false - 1);

            tbVariance_FO.Text = variance_true_kppos.ToString();
            tbVariance_FO_.Text = variance_false_kppos.ToString();

            //standart sapmanın bulunması. (First Occurence)
            standart_deviation_true_kppos = Math.Sqrt(variance_true_kppos);
            standart_deviation_false_kppos = Math.Sqrt(variance_false_kppos);

            tbStandardDev_FO.Text = standart_deviation_true_kppos.ToString();
            tbStandardDev_FO_.Text = standart_deviation_false_kppos.ToString();

            #endregion

            #region Relative Length
            mean_true_rel = 0.0;
            mean_false_rel = 0.0;
            variance_true_rel = 0.0;
            variance_false_rel = 0.0;
            standart_deviation_true_rel = 0.0;
            standart_deviation_false_rel = 0.0;
            toplam_true = 0;
            toplam_false = 0;

            //ortalamanın (mean) bulunması. (Relative Length)   
            foreach (tuple_3 t in KPClass_Value)
            {
                if (t.value == true)
                {
                    toplam_true = toplam_true + t.rellen;
                    //count_true++;
                }
                else if (t.value == false)
                {
                    toplam_false = toplam_false + t.rellen;
                    //count_false++;
                }
            }
            mean_true_rel = toplam_true / ((double)count_true+0.1);
            mean_false_rel = toplam_false / (double)count_false;
            tbMean_Rel.Text = mean_true_rel.ToString();
            tbMean_Rel_.Text = mean_false_rel.ToString();

            //varyansın bulunması. (Relative Length)
            fark_true = 0.0;
            fark_false = 0.0;

            toplam_fark_true = 0.0;
            toplam_fark_false = 0.0;

            foreach (tuple_3 t in KPClass_Value)
            {
                fark_true = 0.0;
                fark_false = 0.0;

                if (t.value == true) fark_true = Math.Abs(t.rellen - mean_true_rel);
                if (t.value == false) fark_false = Math.Abs(t.rellen - mean_false_rel);

                fark_true = fark_true * fark_true;
                fark_false = fark_false * fark_false;

                toplam_fark_true = toplam_fark_true + fark_true;
                toplam_fark_false = toplam_fark_false + fark_false;
            }

            variance_true_rel = toplam_fark_true / ((double)(count_true - 1)+0.1);//+0.1 smoothing için konuldu.
            variance_false_rel = toplam_fark_false / (double)(count_false - 1);

            tbVariance_Rel.Text = variance_true_rel.ToString();
            tbVariance_Rel_.Text = variance_false_rel.ToString();

            //standart sapmanın bulunması. (First Occurence)
            standart_deviation_true_rel = Math.Sqrt(variance_true_rel);
            standart_deviation_false_rel = Math.Sqrt(variance_false_rel);

            tbStandardDev_Rel.Text = standart_deviation_true_rel.ToString();
            tbStandardDev_Rel_.Text = standart_deviation_false_rel.ToString();

            #endregion
        }

        private void btTrainingLearn_Click(object sender, EventArgs e)
        {
            //tf idf
            double mean_true_all = 0.0;
            double mean_false_all = 0.0;
            double variance_true_all = 0.0;
            double variance_false_all = 0.0;
            double standart_deviation_true_all = 0.0;
            double standart_deviation_false_all = 0.0;
            //kp position
            double mean_true_all_kppos = 0.0;
            double mean_false_all_kppos = 0.0;
            double variance_true_all_kppos = 0.0;
            double variance_false_all_kppos = 0.0;
            double standart_deviation_true_all_kppos = 0.0;
            double standart_deviation_false_all_kppos = 0.0;
            //relative length
            double mean_true_all_rel = 0.0;
            double mean_false_all_rel = 0.0;
            double variance_true_all_rel = 0.0;
            double variance_false_all_rel = 0.0;
            double standart_deviation_true_all_rel = 0.0;
            double standart_deviation_false_all_rel = 0.0;

            count_true_all = 0;
            count_false_all = 0;
            for (int i = 1; i < 41; i++)
            {
                DosyalarıOku(i);
                txtParagraphs.Text = alltext;
                dosya = i;

                btCumlelereBol_Click(sender, e);
                btKelimelereBol_Click(sender, e);
                btAdayKpUret_Click(sender, e);
                btKpFiltrele_Click(sender, e);
                btTerimFrekansBul_Click(sender, e);
                btDokumanFrekans_Click(sender, e);
                btTF_IDF_Click(sender, e);
                btFirstOccurence_Click(sender, e);
                btRelativeLength_Click(sender, e);
                btClassValueBul_Click(sender, e);
                btMeanVarianceBul_Click_1(sender, e);

                //tf_idf
                mean_true_all = mean_true_all + mean_true_tfidf;
                mean_false_all = mean_false_all + mean_false_tfidf;

                variance_true_all = variance_true_all + variance_true_tfidf;
                variance_false_all = variance_false_all + variance_false_tfidf;

                standart_deviation_true_all = standart_deviation_true_all + standart_deviation_true_tfidf;
                standart_deviation_false_all = standart_deviation_false_all + standart_deviation_false_tfidf;

                //kp position
                mean_true_all_kppos = mean_true_all + mean_true_kppos;
                mean_false_all_kppos = mean_false_all + mean_false_kppos;

                variance_true_all_kppos = variance_true_all + variance_true_kppos;
                variance_false_all_kppos = variance_false_all + variance_false_kppos;

                standart_deviation_true_all_kppos = standart_deviation_true_all + standart_deviation_true_kppos;
                standart_deviation_false_all_kppos = standart_deviation_false_all + standart_deviation_false_kppos;

                //relative length
                mean_true_all_rel = mean_true_all + mean_true_rel;
                mean_false_all_rel = mean_false_all + mean_false_rel;

                variance_true_all_rel = variance_true_all + variance_true_rel;
                variance_false_all_rel = variance_false_all + variance_false_rel;

                standart_deviation_true_all_rel = standart_deviation_true_all + standart_deviation_true_rel;
                standart_deviation_false_all_rel = standart_deviation_false_all + standart_deviation_false_rel;
            }
            //tf idf
            double final_mean_true = mean_true_all / 40;
            double final_mean_false = mean_false_all / 40;
            tbMeanAll.Text = final_mean_true.ToString();
            tbMeanAll_.Text = final_mean_false.ToString();
            double final_variance_true = variance_true_all / 40;
            double final_variance_false = variance_false_all / 40;
            tbVarianceAll.Text = final_variance_true.ToString();
            tbVarianceAll_.Text = final_variance_false.ToString();
            double final_standart_deviation_true = standart_deviation_true_all / 40;
            double final_standart_deviation_false = standart_deviation_false_all / 40;
            tbStandardDevAll.Text = final_standart_deviation_true.ToString();
            tbStandardDevAll_.Text = final_standart_deviation_false.ToString();
            //kp position
            double final_mean_true_kppos = mean_true_all_kppos / 40;
            double final_mean_false_kppos = mean_false_all_kppos / 40;
            tbMeanAll_KPPos.Text = final_mean_true_kppos.ToString();
            tbMeanAll_KPPos_.Text = final_mean_false_kppos.ToString();
            double final_variance_true_kppos = variance_true_all_kppos / 40;
            double final_variance_false_kppos = variance_false_all_kppos / 40;
            tbVarianceAll_KPPos.Text = final_variance_true_kppos.ToString();
            tbVarianceAll_KPPos_.Text = final_variance_false_kppos.ToString();
            double final_standart_deviation_true_kppos = standart_deviation_true_all_kppos / 40;
            double final_standart_deviation_false_kppos = standart_deviation_false_all_kppos / 40;
            tbStandardDevAll_KPPos.Text = final_standart_deviation_true_kppos.ToString();
            tbStandardDevAll_KPPos_.Text = final_standart_deviation_false_kppos.ToString();
            //relative length
            double final_mean_true_rel = mean_true_all_rel / 40;
            double final_mean_false_rel = mean_false_all_rel / 40;
            tbMeanAll_Rel.Text = final_mean_true_rel.ToString();
            tbMeanAll_Rel_.Text = final_mean_false_rel.ToString();
            double final_variance_true_rel = variance_true_all_rel / 40;
            double final_variance_false_rel = variance_false_all_rel / 40;
            tbVarianceAll_Rel.Text = final_variance_true_rel.ToString();
            tbVarianceAll_Rel_.Text = final_variance_false_rel.ToString();
            double final_standart_deviation_true_rel = standart_deviation_true_all_rel / 40;
            double final_standart_deviation_false_rel = standart_deviation_false_all_rel / 40;
            tbStandardDevAll_Rel.Text = final_standart_deviation_true_rel.ToString();
            tbStandardDevAll_Rel_.Text = final_standart_deviation_false_rel.ToString();

            tbTrue.Text = count_true_all.ToString();
            tbFalse.Text = count_false_all.ToString();
        }

        private void btTestDataModelUygula_Click(object sender, EventArgs e)
        {
            if ((Int32.Parse(tbBas.Text)<1) && (Int32.Parse(tbBas.Text)>20))
            { MessageBox.Show("Başlangıç Doküman No. 1 ile 20 arasında olabilir!"); return; }
            if ((Int32.Parse(tbBit.Text) < 1) && (Int32.Parse(tbBit.Text) > 20))
            { MessageBox.Show("Bitiş Doküman No. 1 ile 20 arasında olabilir!"); return; }
            if ((cbKP5.Checked==false) && (cbKP10.Checked==false))
            {MessageBox.Show("Lütfen anahtar kelime sayısını seçiniz."); return;}
            
             if (cbKP5.Checked==true)
                TestDataImplement(sender, e,5);
             else if (cbKP10.Checked==true)
                TestDataImplement(sender, e,10);
        }

        private void fmKeyPhrase_FormClosed(object sender, FormClosedEventArgs e)
        {
            alltext = "";
            Application.Exit();
        }

        private void btTemizleTFIDF_Click(object sender, EventArgs e)
        {
            dgvTermFreq.Rows.Clear();
            dgvDocFreq.Rows.Clear();
            dgvTF_IDF.Rows.Clear();
        }

        private void btTemizleRelLen_Click(object sender, EventArgs e)
        {
            dgvFirstOccurence.Rows.Clear();
            dgvRelativeLength.Rows.Clear();
        }

        private void btDosyaAc_Main_Click(object sender, EventArgs e)
        {
            btDosyaAc_Click(sender, e);
        }

        private void btProgramGiris_Click(object sender, EventArgs e)
        {
            //yes ve no sayılarının atanması.
            int count_true = Int32.Parse(tbTrue.Text);
            int count_false = Int32.Parse(tbFalse.Text);

            if (txtParagraphs.Text.Trim() != rtbOzetMain.Text)
                txtParagraphs.Text = rtbOzetMain.Text.Trim();

            btCumlelereBol_Click(sender, e);
            btKelimelereBol_Click(sender, e);
            btAdayKpUret_Click(sender, e);
            btKpFiltrele_Click(sender, e);
            btTerimFrekansBul_Click(sender, e);
            btDokumanFrekans_Click(sender, e);
            btTF_IDF_Click(sender, e);
            btFirstOccurence_Click(sender, e);
            btRelativeLength_Click(sender, e);

            //tfidf, kpposition ve relative lentgh özelliklerinin P(probability) değerlerinin bulunması.
            //Probability değerlerini normal distribution formülü ile buluyoruz.
            double P_tfidf = 0.0;
            double P_kppos = 0.0;
            double P_rellen = 0.0;

            double P_tfidf_ = 0.0;
            double P_kppos_ = 0.0;
            double P_rellen_ = 0.0;

            double P_yes = 0.0;
            double P_no = 0.0;
            double P_final = 0.0;
            KPFinal_Puan.Clear();

            foreach (tuple_5 t in KPRelLength)
            {
                //yes parametreleri...
                P_tfidf = 0.0;
                P_tfidf = (1 / ((Math.Sqrt(2 * Math.PI)) * Double.Parse(tbVarianceAll.Text.ToString()))) *
                            (Math.Pow(Math.E, Math.Pow((t.tfidf - Double.Parse(tbMeanAll.Text.ToString())), 2) / (2 * Double.Parse(tbStandardDevAll.Text.ToString())) * -1));
                P_kppos = 0.0;
                P_kppos = (1 / ((Math.Sqrt(2 * Math.PI)) * Double.Parse(tbVarianceAll_KPPos.Text.ToString()))) *
                            (Math.Pow(Math.E, Math.Pow((t.firstocc - Double.Parse(tbMeanAll_KPPos.Text.ToString())), 2) / (2 * Double.Parse(tbStandardDevAll_KPPos.Text.ToString())) * -1));
                P_rellen = 0.0;
                P_rellen = (1 / ((Math.Sqrt(2 * Math.PI)) * Double.Parse(tbVarianceAll_Rel.Text.ToString()))) *
                            (Math.Pow(Math.E, Math.Pow((t.rellength - Double.Parse(tbMeanAll_Rel.Text.ToString())), 2) / (2 * Double.Parse(tbStandardDevAll_Rel.Text.ToString())) * -1));

                P_yes = 0.0;
                P_yes = ((double)count_true / (count_true + count_false)) * P_tfidf * P_kppos * P_rellen;

                //no parametreleri...
                P_tfidf_ = 0.0;
                P_tfidf_ = (1 / ((Math.Sqrt(2 * Math.PI)) * Double.Parse(tbVarianceAll_.Text.ToString()))) *
                            (Math.Pow(Math.E, Math.Pow((t.tfidf - Double.Parse(tbMeanAll_.Text.ToString())), 2) / (2 * Double.Parse(tbStandardDevAll_.Text.ToString())) * -1));
                P_kppos_ = 0.0;
                P_kppos_ = (1 / ((Math.Sqrt(2 * Math.PI)) * Double.Parse(tbVarianceAll_KPPos_.Text.ToString()))) *
                            (Math.Pow(Math.E, Math.Pow((t.firstocc - Double.Parse(tbMeanAll_KPPos_.Text.ToString())), 2) / (2 * Double.Parse(tbStandardDevAll_KPPos_.Text.ToString())) * -1));
                P_rellen_ = 0.0;
                P_rellen_ = (1 / ((Math.Sqrt(2 * Math.PI)) * Double.Parse(tbVarianceAll_Rel_.Text.ToString()))) *
                            (Math.Pow(Math.E, Math.Pow((t.rellength - Double.Parse(tbMeanAll_Rel_.Text.ToString())), 2) / (2 * Double.Parse(tbStandardDevAll_Rel_.Text.ToString())) * -1));

                P_no = 0.0;
                P_no = ((double)count_false / (count_true + count_false)) * P_tfidf_ * P_kppos_ * P_rellen;

                //son probability...
                P_final = 0.0;
                P_final = P_yes / (P_yes + P_no);

                KPFinal_Puan.Add(new tuple { kp = t.kp, tfidf = P_final });
            }

            KPFinal_Puan = KPFinal_Puan.OrderByDescending(a => a.tfidf).ToList();

            int kp_sayi = 0;
            int varmi = 0;
            lbAnahtarMain.Items.Clear();
            Final_KPs.Clear();

            if (cbTekrar.Checked == false)
            {
                foreach (tuple t in KPFinal_Puan)
                {
                    kp_sayi++;
                    Final_KPs.Add(t.kp.Trim());
                    if (kp_sayi == 10)
                        break;
                }

                foreach (string t in Final_KPs)
                {
                    lbAnahtarMain.Items.Add(t);
                }
            }
            else if (cbTekrar.Checked == true)
            {
                Final_KPs.Add("Anahtar Kelimeler:");
                foreach (tuple t in KPFinal_Puan)
                {
                    kp_sayi++;
                    varmi = 0;
                    foreach (string s in Final_KPs)
                    {
                        if (t.kp.Contains(s))
                        {
                            varmi = 0;
                            break;
                        }
                        else varmi = 1;
                    }
                    if (varmi == 1) Final_KPs.Add(t.kp);
                    if (kp_sayi == (3 * 10))
                        break;
                }
                List<string> F = Final_KPs.Distinct().ToList();
                F.RemoveAt(0);

                kp_sayi = 0;
                foreach (string t in F)
                {
                    kp_sayi++;
                    lbAnahtarMain.Items.Add(t);
                    if (kp_sayi == 10)
                    { lbKp_Extracted.Items.Add(""); break; }
                }
            }

        }
    }

}
