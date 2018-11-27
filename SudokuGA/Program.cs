using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace SudokuGA
{
    class Program
    {
        //. http://www.sudokugame.org/

        /// <summary>
        /// Sudoku Solver
        /// </summary>
        /// <param name="boardstr"></param>
        public static void SolveSudoku(string boardstr)
        {
            //. initialize board ( blank is marked 100 and hints are marked over 100 )
            int hints = 0;
            int[,] board = new int[9, 9];
            for (int i = 0; i < 9; i++) {
                for (int j = 0; j < 9; j++) {
                    if (boardstr[i * 9 + j] == '.') {
                        board[i, j] = 100;
                    }
                    else {
                        board[i, j] = int.Parse(boardstr.Substring(i * 9 + j, 1)) + 100;
                        hints++;
                    }
                }
            }


            //. fill cell
            for (int k = 0; k < 9; k++) {
                int[] cell = new int[9];
                for (int i = 0; i < 3; i++) {
                    for (int j = 0; j < 3; j++) {
                        cell[i * 3 + j] = board[(k / 3) * 3 + i, (k % 3) * 3 + j];
                    }
                }
                List<int> set = new List<int>(new int[] { 100, 101, 102, 103, 104, 105, 106, 107, 108, 109 });
                List<int> pat = new List<int>(cell);
                var diff = set.Except(pat).ToList();
                int idx = 0;
                for (int i = 0; i < 3; i++) {
                    for (int j = 0; j < 3; j++) {
                        if (board[(k / 3) * 3 + i, (k % 3) * 3 + j] == 100) {
                            board[(k / 3) * 3 + i, (k % 3) * 3 + j] = diff[idx++] - 100;
                        }
                    }
                }
            }

            int MAX_GEN = 5000;

            Random rand = new Random();

            List<int[,]> all = new List<int[,]>();

            Console.WriteLine("Hints are {0}", hints);

            //. Popuration.
            for (int i = 0; i < MAX_GEN; i++) {
                all.Add(Population(board));
            }

            //. initial pool
            all = all.OrderBy(r => Fitness(r)).ToList();

            int generation = 0;
            int mutantPercent = 30;
            int score = -1;
            int peace = 0;
            int mutationBlocks = 5;
            int mutationTimes = 1;

            if (hints <= 20) {
                mutationBlocks = 9;
                mutationTimes = 2;
            }

            do {
                //. random marriage
                List<int[,]> children = new List<int[,]>();
                int[] couples = new int[MAX_GEN];
                for (int i = 0; i < MAX_GEN; i++) {
                    couples[i] = i;
                }

                for (int i = 0; i < MAX_GEN; i++) {
                    int pa = rand.Next() % MAX_GEN;
                    int pb = rand.Next() % MAX_GEN;
                    int t = couples[pa];
                    couples[pa] = couples[pb];
                    couples[pb] = t;
                }

                for( int i=0; i < MAX_GEN; i+=2) {
                    children.Add(CrossOver(all[couples[i]], all[couples[i + 1]]));
                }

                all.AddRange(children);

                //. new generation
                all = all.OrderBy(g => Fitness(g)).Take(MAX_GEN).ToList();

                int cur = Fitness(all[0]);
                int lowest = Fitness(all[MAX_GEN-1]);

                Console.WriteLine("Generation {0} : Highest {1} / Lowest {2}", generation, cur, lowest);
                Dump(all[0]);

                //. solved?
                if ( cur == score) {
                    peace++;
                } else {
                    peace = 0;
                }

                if ( (score == 2) && (peace > 80) ) {
                    //. Armageddon!!
                    for (int i = 0; i < MAX_GEN; i++) {
                        Armageddon(all[i]);
                    }
                } else {
                    //. update highest score
                    score = cur;

                    //. add mutation and next generation
                    for (int i = 0; i < MAX_GEN; i++) {
                        if ((rand.Next() % 100) < mutantPercent) {
                            Mutation(all[i], mutationBlocks, mutationTimes);
                        }
                    }
                }
                generation++;
            } while (score != 0);

            Console.WriteLine("Solved!!");
        }

        /// <summary>
        /// Population the specified board.
        /// </summary>
        /// <returns>The popuration.</returns>
        /// <param name="board">Board.</param>
        public static int[,] Population(int[,] board)
        {
            Random rand = new Random((int)DateTime.Now.Ticks);

            var t = board.DeepClone();
            for (int ii = 0; ii < 9; ii++) {
                for (int j = 0; j < 27; j++) {
                    int bri = (ii / 3) * 3;
                    int bci = (ii % 3) * 3;
                    int swapPointAR = rand.Next() % 3;
                    int swapPointAC = rand.Next() % 3;
                    int swapPointBR = rand.Next() % 3;
                    int swapPointBC = rand.Next() % 3;
                    if (t[bri + swapPointAR, bci + swapPointAC] >= 100 || t[bri + swapPointBR, bci + swapPointBC] > 100) {
                        //. nothing to do
                        j--;
                        continue;
                    }
                    var tmp = t[bri + swapPointAR, bci + swapPointAC];
                    t[bri + swapPointAR, bci + swapPointAC] = t[bri + swapPointBR, bci + swapPointBC];
                    t[bri + swapPointBR, bci + swapPointBC] = tmp;
                }
            }
            return t;
        }

        /// <summary>
        /// Mutation
        /// </summary>
        /// <param name="gen"></param>
        public static void Mutation(int[,] gen, int mutationBlocks, int mutationTimes)
        {
            Random rand = new Random();

            for (int ii = 0; ii < mutationBlocks; ii++) {
                int mb = rand.Next() % 9;
                for (int j = 0; j < mutationTimes; j++) {
                    int br = (mb / 3) * 3;
                    int bc = (mb % 3) * 3;
                    int spra = rand.Next() % 3;
                    int spca= rand.Next() % 3;
                    int sprb = rand.Next() % 3;
                    int spcb = rand.Next() % 3;
                    if (gen[br + spra, bc + spca] >= 100 || gen[br + sprb, bc + spcb] >= 100) {
                        //. nothing to do
                        j--;
                        continue;
                    }

                    bool canSwap = true;
                    var val = gen[br + spra, bc + spca] + 100;
                    for ( int k=0; k < 9; k++) {
                        if ( gen[k, bc + spcb] == val) {
                            canSwap = false;
                            break;
                        }
                    }
                    if ( !canSwap ) {
                        continue;
                    }
                    for (int k = 0; k < 9; k++) {
                        if (gen[br + sprb, k] == val) {
                            canSwap = false;
                            break;
                        }
                    }
                    if (!canSwap) {
                        continue;
                    }
                    var tmp = gen[br + spra, bc + spca];
                    gen[br + spra, bc + spca] = gen[br + sprb, bc + spcb];
                    gen[br + sprb, bc + spcb] = tmp;
                }
            }
        }

        /// <summary>
        /// Armageddon the specified gen.
        /// </summary>
        /// <param name="gen">Gen.</param>
        public static void Armageddon( int[,] gen)
        {
            Random rand = new Random((int)DateTime.Now.Ticks);

            for (int ii = 0; ii < 9; ii++) {
                int mb = ii;
                for (int j = 0; j < 4; j++) {
                    int bri = (mb / 3) * 3;
                    int bci = (mb % 3) * 3;
                    int swapPointAR = rand.Next() % 3;
                    int swapPointAC = rand.Next() % 3;
                    int swapPointBR = rand.Next() % 3;
                    int swapPointBC = rand.Next() % 3;
                    if (gen[bri + swapPointAR, bci + swapPointAC] >= 100 || gen[bri + swapPointBR, bci + swapPointBC] >= 100) {
                        //. nothing to do
                        j--;
                        continue;
                    }
                    if ((swapPointAC == swapPointBC) && (swapPointAR == swapPointBR)) {
                        //. nothing to do
                        j--;
                        continue;
                    }
                    var tmp = gen[bri + swapPointAR, bci + swapPointAC];
                    gen[bri + swapPointAR, bci + swapPointAC] = gen[bri + swapPointBR, bci + swapPointBC];
                    gen[bri + swapPointBR, bci + swapPointBC] = tmp;
                }
            }

        }

        /// <summary>
        /// Cross Over
        /// </summary>
        /// <param name="genA"></param>
        /// <param name="genB"></param>
        /// <returns></returns>
        public static int[,] CrossOver(int[,] genA, int[,] genB)
        {
            Random rand = new Random();

            int[,] newGen = genB.DeepClone();

            List<int> blocks = new List<int>();

            for (int i = 0; i < 2; i++) {
                int bid = rand.Next() % 9;
                if (blocks.IndexOf(bid) >= 0) {
                    i--;
                    continue;
                }
                blocks.Add(bid);
                int br = (bid / 3) * 3;
                int bc = (bid % 3) * 3;

                for (int j = 0; j < 3; j++) {
                    for (int k = 0; k < 3; k++) {
                        newGen[br + j, bc + k] = genA[br + j, bc + k];
                    }
                }
            }

            return newGen;
        }

        /// <summary>
        /// Calculate fitness
        /// </summary>
        /// <param name="gen"></param>
        /// <returns></returns>
        public static int Fitness(int[,] gen)
        {
            int score = 0;
            List<int> set = new List<int>(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            for (int i = 0; i < 9; i++) {
                List<int> row = new List<int>();
                for (int j = 0; j < 9; j++) row.Add(gen[i, j] >= 100 ? gen[i, j] - 100 : gen[i, j]);
                score += set.Except(row).Count();
            }

            for (int i = 0; i < 9; i++) {
                List<int> col = new List<int>();
                for (int j = 0; j < 9; j++) col.Add(gen[j, i] >= 100 ? gen[j, i] - 100 : gen[j, i]);
                score += set.Except(col).Count();
            }

            return score;
        }

        /// <summary>
        /// Dump Sudoku Matrix
        /// </summary>
        /// <param name="gen"></param>
        public static void Dump(int[,] gen)
        {
            Console.WriteLine("DUMP GEN");
            for (int i = 0; i < 9; i++) {
                for (int j = 0; j < 9; j++) {
                    Console.Write("{0, 3} ", gen[i, j]);
                }
                Console.WriteLine();
            }
        }

        //. Entry point
        static void Main(string[] args)
        {
            StreamReader sr = new StreamReader(args[0]);
            string question = sr.ReadToEnd();
            sr.Close();
            question = Regex.Replace(question, "[\n\r]", "");
            SolveSudoku(question);
            Console.ReadLine();
        }
    }

    public static class Extentions
    {
        public static T DeepClone<T>(this T src)
        {
            using (var memoryStream = new System.IO.MemoryStream()) {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, src);
                memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
                return (T)binaryFormatter.Deserialize(memoryStream);
            }
        }
    }

}
