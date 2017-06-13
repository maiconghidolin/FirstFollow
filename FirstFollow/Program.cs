using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirstFollow {
    class Program {

        static string caminhoGramatica = "";
        static string simboloAtribuicao = "::=";

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
                novaRegra.label = linha.Substring(0, indicesimboloAtribuicao);
                string[] listaProducoes = linha.Substring(indicesimboloAtribuicao + simboloAtribuicao.Length).Replace(" ", string.Empty).Split('|');
                novaRegra.producoes = listaProducoes.ToList();
                Regras.Add(novaRegra);
            }
            stream.Close();
            stream.Dispose();
            return Regras;
        }

        public static void geraConjuntosFirst(List<FirstFollow> listaConjuntosFirstFollow, List<Regra> Regras) {

        }

        public static void geraConjuntosFollow(List<FirstFollow> listaConjuntosFirstFollow, List<Regra> Regras) {

        }

        public class Regra {
            public string label;
            public List<string> producoes;
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
