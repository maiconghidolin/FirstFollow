using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstFollow {
    class Program {

        static string caminhoGramatica = "";
        static string simboloAtribuicao = "::=";
        static string la_bracket = "<";
        static string ra_bracket = ">";
        static char epsilon = '~';

        static void log(string mensagem, bool fim) {
            Console.WriteLine(mensagem);
            if (fim) {
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        static void Main(string[] args) {
            if (args == null || args.Length < 1) {
                log("Faltando argumentos... Pressione alguma tecla para encerrar!", true);
            }
            caminhoGramatica = args[0];
            if (!System.IO.File.Exists(caminhoGramatica)) { 
                log("Gramática não localizada... Pressione alguma tecla para encerrar!", true);
            }
            List<Regra> Regras = lerGramatica();
            List<FirstFollow> listaConjuntosFirstFollow = new List<FirstFollow>();
            geraConjuntosFirst(listaConjuntosFirstFollow, Regras);
            geraConjuntosFollow(listaConjuntosFirstFollow, Regras);
            log("Estado\t\tFIRST - FOLLOW", false);
            foreach (FirstFollow conjuntos in listaConjuntosFirstFollow) {
                log(conjuntos.label + "\t\t" + conjuntos.firstToString() + "\t\t" + conjuntos.followToString(), false);
            }
            log("Pressione algo para encerrar!...", true);
        }

        public static List<Regra> lerGramatica() {
            List<Regra> Regras = new List<Regra>();
            System.IO.StreamReader stream = new System.IO.StreamReader(caminhoGramatica);
            while (!stream.EndOfStream) {
                string linha = stream.ReadLine();
                int indicesimboloAtribuicao = linha.IndexOf(simboloAtribuicao);
                Regra novaRegra = new Regra();
                // separa o simbolo que da nome a regra
                novaRegra.label = linha.Substring(0, indicesimboloAtribuicao).Replace(" ", String.Empty);
                novaRegra.producoes = new List<string[]>();
                // separa cada produção
                string[] listaProducoes = linha.Substring(indicesimboloAtribuicao + simboloAtribuicao.Length).Replace(" ", string.Empty).Split('|');
                List<string> producoesAux = new List<string>();
                foreach (string str in listaProducoes) {
                    // para cada produção, coloca cada simbolo (terminais ou nao terminais) em uma posicao do vetor
                    for (int i = 0; i < str.Length; i++) {
                        if (str[i].Equals(la_bracket[0])) {
                            producoesAux.Add(str.Substring(i, str.IndexOf(ra_bracket, i) - i + 1));
                            i = str.IndexOf(ra_bracket, i);
                        } else
                            producoesAux.Add(str.Substring(i, 1));
                    }
                    novaRegra.producoes.Add(producoesAux.ToArray());
                    producoesAux.Clear();
                }
                Regras.Add(novaRegra);
            }
            stream.Close();
            stream.Dispose();
            return Regras;
        }

        public static void geraConjuntosFirst(List<FirstFollow> listaConjuntosFirstFollow, List<Regra> regras) {
            log("Gerando conjunto FIRST...", false);
            foreach (var regra in regras) {
                // para cada regra já adiciona no conjunto first os simbolos terminais
                FirstFollow ff = new FirstFollow(regra.label);
                ff.first.AddRange(regra.producoes.Where(x => !x[0].StartsWith(la_bracket)).Select(x => x[0].ToString()).Distinct().ToList());
                listaConjuntosFirstFollow.Add(ff);
                log(String.Format("FIRST({0}) = FIRST({0}) + {1}...", regra.label, string.Join(" ", ff.first)), false);
            }
            bool mudou = true;
            while (mudou) {
                mudou = false;
                for (int i = 0; i < regras.Count(); i++) {
                    List<string[]> producoesComecamNaoTerminais = regras[i].producoes.Where(x => x[0].StartsWith(la_bracket)).ToList();
                    // examina cada produção que começa com símbolo não terminal
                    foreach (string[] nt in producoesComecamNaoTerminais) {
                        log("Analisando produção " + string.Join("", nt) + " de " + regras[i].label + "...", false);
                        for (int j = 0; j < nt.Length; j++) {
                            // só cai aqui quando um terminal é precedido por um nao terminal que possui epsilon em seu conjunto first
                            if (!nt[j].StartsWith(la_bracket)) {
                                if (!listaConjuntosFirstFollow[i].first.Contains(nt[j])) {
                                    listaConjuntosFirstFollow[i].first.Add(nt[j]);
                                    log(String.Format("FIRST({0}) = FIRST({0}) + {1}...", listaConjuntosFirstFollow[i].label, nt[j]), false);
                                    mudou = true;
                                }
                                break;
                            }
                            FirstFollow firstFollowNT = FirstFollowSimbolo(listaConjuntosFirstFollow, nt[j]);
                            // se o first do símbolo não terminal existe
                            if (firstFollowNT != null && firstFollowNT.first.Count > 0) {
                                // copia para o first do simbolo que da nome a regra o first do símbolo não terminal 
                                // mesmo que o simbolo que da nome a regra é o mesmo que o da produção,, tem que copiar
                                // pois pode ser que tenha um epsilon
                                log(String.Format("FIRST({0}) = FIRST({0}) + FIRST({1})...", listaConjuntosFirstFollow[i].label, firstFollowNT.label), false);
                                if (union(ref listaConjuntosFirstFollow[i].first, firstFollowNT.first, false)) {
                                    mudou = true;
                                }
                                // se o first copiado não tem epsilon vai para a próxima produção
                                // senão tem que verificar o próximo símbolo da produção atual
                                if (!firstFollowNT.first.Exists(x => x.Contains(epsilon.ToString()))) {
                                    break;
                                } else {
                                    if (j == nt.Length - 1) {
                                        if (!listaConjuntosFirstFollow[i].first.Contains(epsilon.ToString())) {
                                            listaConjuntosFirstFollow[i].first.Add(epsilon.ToString());
                                            log(String.Format("FIRST({0}) = FIRST({0}) + {1}...", listaConjuntosFirstFollow[i].label, epsilon.ToString()), false);
                                            mudou = true;
                                        }
                                    }
                                }
                            } else {
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static void geraConjuntosFollow(List<FirstFollow> listaConjuntosFirstFollow, List<Regra> regras) {
            log("Gerando a primeira etapa do conjunto FOLLOW...", false);
            listaConjuntosFirstFollow[0].follow = new List<string>() { "$" };
            foreach (Regra regra in regras) {
                foreach (string[] producao in regra.producoes) {
                    log("Analisando produção " + string.Join("", producao) + " de " + regra.label + "...", false);
                    for (int i = 0; i < producao.Length - 1; i++) {
                        // verifica se cada simbolo de cada producao de cada regra é não terminal
                        if (producao[i].StartsWith(la_bracket)) {
                            // se é nao terminal, pega o conjunto follow dele
                            FirstFollow firstFollow1 = FirstFollowSimbolo(listaConjuntosFirstFollow, producao[i]);
                            if (producao[i + 1].StartsWith(la_bracket)) {
                                // se o próximo simbolo é não terminal pega o conj. first e une com o follow do atual
                                log(String.Format("FOLLOW({0}) = FOLLOW({0}) + FIRST({1})...", producao[i], producao[i + 1]), false);
                                FirstFollow firstFollow2 = FirstFollowSimbolo(listaConjuntosFirstFollow, producao[i + 1]);
                                union(ref firstFollow1.follow, firstFollow2.first, false);
                            } else {
                                log(String.Format("FOLLOW({0}) = FOLLOW({0}) + {1}...", producao[i], producao[i + 1]), false);
                                if (!firstFollow1.follow.Contains(producao[i + 1])) {
                                    firstFollow1.follow.Add(producao[i + 1]);
                                }
                            }
                        }
                    }
                }
            }
            log("Gerando a segunda etapa do conjunto FOLLOW...", false);
            bool mudou = true;
            while (mudou) {
                mudou = false;
                foreach (Regra regra in regras) {
                    log("Analisando regra " + regra.label + "...", false);
                    // conjunto first/follow do simbolo que da nome a regra
                    FirstFollow firstFollowRegra = FirstFollowSimbolo(listaConjuntosFirstFollow, regra.label);
                    List<string[]> producoesTerminamNaoTerminais = regra.producoes.Where(x => x[x.Length - 1].StartsWith(la_bracket)).ToList();
                    if(producoesTerminamNaoTerminais.Count == 0) {
                        log("Nenhuma produção acaba com não terminal...",false);
                    }
                    // examina cada produção que termina com símbolo não terminal
                    foreach (string[] simbolos in producoesTerminamNaoTerminais) {
                        for (int i = simbolos.Length - 1; i >= 0; i--) {
                            // conjunto first/follow do último simbolo da produção
                            FirstFollow firstFollowUltimo = FirstFollowSimbolo(listaConjuntosFirstFollow, simbolos[i]);
                            if(firstFollowUltimo == null) {
                                // só cai aqui quando um terminal aparece antes de um nao terminal que possui epsilon em seu conjunto first
                                log("Símbolo " + simbolos[i] + " é terminal..." ,false);
                                break;
                            }
                            if (firstFollowRegra.label == firstFollowUltimo.label) {
                                log("O último símbolo (" + simbolos[i] + ") é o mesmo que dá nome a regra...",false);
                            } else {
                                log(String.Format("Produção {0}: FOLLOW({1}) = FOLLOW({1}) + FOLLOW({2})...", simbolos[i], firstFollowUltimo.label, firstFollowRegra.label), false);
                                if (union(ref firstFollowUltimo.follow, firstFollowRegra.follow, false)) {
                                    mudou = true;
                                }
                            }
                            if (!firstFollowUltimo.first.Contains(epsilon.ToString())) {
                                log(simbolos[i] + " não contém épsilon no seu conjunto First.", false);
                                break;
                            }
                        }
                    }
                }
            }
        }

        static FirstFollow FirstFollowSimbolo(List<FirstFollow> listaFirstFollow, string label) {
            FirstFollow firstFollow = listaFirstFollow.Where(x => x.label.Equals(label)).FirstOrDefault();
            return firstFollow;
        }

        static bool union(ref List<string> lista1, List<string> lista2, bool consideraepsilon) {
            int tamanhoInicialLista1 = lista1.Count;
            lista1 = lista1.Union(!consideraepsilon ? lista2.Where(x => !x.Equals(epsilon.ToString())) : lista2).ToList();
            return lista1.Count > tamanhoInicialLista1; 
        }

        public class Regra {
            public string label;
            public List<string[]> producoes;
        }

        public class FirstFollow {
            public string label;
            public List<string> first;
            public List<string> follow;

            public FirstFollow(string label) {
                this.label = label;
                first = new List<string>();
                follow = new List<string>();
            }

            public string firstToString() {
                return string.Join(",", first);
            }

            public string followToString() {
                return string.Join(",", follow);
            }

        }

    }
}
