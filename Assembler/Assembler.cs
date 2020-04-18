
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Assembler
{
    public class Assembler
    {
        private const int WORD_SIZE = 16;

        private Dictionary<string, int[]> m_dControl, m_dJmp, m_dest; //these dictionaries map command mnemonics to machine code - they are initialized at the bottom of the class
        private  Dictionary <string, int> labelDictionary;
        //more data structures here (symbol map, ...)

        public Assembler()
        {
            InitCommandDictionaries();
        }

        //this method is called from the outside to run the assembler translation
        public void TranslateAssemblyFile(string sInputAssemblyFile, string sOutputMachineCodeFile)
        {
            //read the raw input, including comments, errors, ...
            StreamReader sr = new StreamReader(sInputAssemblyFile);
            List<string> lLines = new List<string>();
            while (!sr.EndOfStream)
            {
                lLines.Add(sr.ReadLine());
            }
            sr.Close();
            //translate to machine code
            List<string> lTranslated = TranslateAssemblyFile(lLines);
            //write the output to the machine code file
            StreamWriter sw = new StreamWriter(sOutputMachineCodeFile);
            foreach (string sLine in lTranslated)
                sw.WriteLine(sLine);
            sw.Close();
        }

        //translate assembly into machine code
        private List<string> TranslateAssemblyFile(List<string> lLines)
        {
            //implementation order:
            //first, implement "TranslateAssemblyToMachineCode", and check if the examples "Add", "MaxL" are translated correctly.
            //next, implement "CreateSymbolTable", and modify the method "TranslateAssemblyToMachineCode" so it will support symbols (translating symbols to numbers). check this on the examples that don't contain macros
            //the last thing you need to do, is to implement "ExpendMacro", and test it on the example: "SquareMacro.asm".
            //init data structures here 

            //expand the macros
            List<string> lAfterMacroExpansion = ExpendMacros(lLines);

            //first pass - create symbol table and remove lable lines
            CreateSymbolTable(lAfterMacroExpansion);

            //second pass - replace symbols with numbers, and translate to machine code
            List<string> lAfterTranslation = TranslateAssemblyToMachineCode(lAfterMacroExpansion);
            return lAfterTranslation;
        }

        
        //first pass - replace all macros with real assembly
        private List<string> ExpendMacros(List<string> lLines)
        {
            //You do not need to change this function, you only need to implement the "ExapndMacro" method (that gets a single line == string)
            List<string> lAfterExpansion = new List<string>();
            for (int i = 0; i < lLines.Count; i++)
            {
                //remove all redudant characters
                string sLine = CleanWhiteSpacesAndComments(lLines[i]);
                if (sLine == "")
                    continue;
                //if the line contains a macro, expand it, otherwise the line remains the same
                List<string> lExpanded = ExapndMacro(sLine);
                //we may get multiple lines from a macro expansion
                foreach (string sExpanded in lExpanded)
                {
                    lAfterExpansion.Add(sExpanded);
                }
            }
            return lAfterExpansion;
        }

        //expand a single macro line
        private List<string> ExapndMacro(string sLine)
        {
            int num = -1;
            List<string> lExpanded = new List<string>();


            if (IsCCommand(sLine))
            {
                string sDest, sCompute, sJmp;
                GetCommandParts(sLine, out sDest, out sCompute, out sJmp);
                //your code here - check for indirect addessing and for jmp shortcuts
                //read the word file to see all the macros you need to support

                if (sCompute.Contains("++"))
                {
                    string dest = sCompute.Substring(0, sCompute.IndexOf((char)'+'));
                    if (m_dest.ContainsKey(dest))
                        lExpanded.Add(dest + "=" + dest + "+1");
                    else
                    {
                        lExpanded.Add("@" + dest);
                        lExpanded.Add("M=M+1");
                    }
                }
                if (sCompute.Contains("--"))
                {
                    string dest = sCompute.Substring(0, sCompute.IndexOf((char)'-'));
                    if (m_dest.ContainsKey(dest))
                        lExpanded.Add(dest + "=" + dest + "-1");
                    else
                    {
                        lExpanded.Add("@" + dest);
                        lExpanded.Add("M=M-1");
                    }
                }
                if (sLine.Contains(':'))
                {
                    lExpanded.Add("@" + sJmp.Substring(sJmp.IndexOf((char)':') + 1));
                    lExpanded.Add(sCompute + ";" + sJmp.Substring(0, sJmp.IndexOf((char)':')));
                }
                if (sLine.Contains('=') && Int32.TryParse(sCompute, out num) && !m_dControl.ContainsKey(sCompute))
                {
                    if (!(m_dest.ContainsKey(sDest)))
                    {
                        lExpanded.Add("@" + sCompute);
                        lExpanded.Add("D=A");
                        lExpanded.Add("@" + sDest);
                        lExpanded.Add("M=D");
                    }
                    else
                    {
                        lExpanded.Add("@" + sCompute);
                        lExpanded.Add(sDest + "=A");
                    }
                }
                if (!m_dest.ContainsKey(sDest) && m_dControl.ContainsKey(sCompute))
                {
                    lExpanded.Add("@" + sDest);
                    lExpanded.Add("M=" + sCompute);
                }
                if (m_dest.ContainsKey(sDest) && !m_dControl.ContainsKey(sCompute))
                {
                    lExpanded.Add("@" + sCompute);
                    lExpanded.Add(sDest + "=M");
                }
                if (!m_dest.ContainsKey(sDest) && !m_dControl.ContainsKey(sCompute))
                {
                    lExpanded.Add("@" + sCompute);
                    lExpanded.Add("D=M");
                    lExpanded.Add("@" + sDest);
                    lExpanded.Add("M=D");
                }
            }
                if (lExpanded.Count < 1)
                    lExpanded.Add(sLine);
            return lExpanded;
        }

        //second pass - record all symbols - labels and variables
        private void CreateSymbolTable(List<string> lLines)
        {
           int counterOftabel = 0, numToParse;
            labelDictionary = new Dictionary<string, int>();// Symbel Dic

            for (int i = 0; i < 16; i++)
            {
               String myString = i.ToString();
                labelDictionary["R" + myString] = i;
                if (i == 15)
                    counterOftabel = i+1;
            }
            labelDictionary["SCREEN"] = 16384;
            labelDictionary["KYB"] = 24576;

            List<string> labelArray = new List<string>();
            int sumCounts = lLines.Count, curLine = 0;
            while (curLine < sumCounts)
            {
                if (!(IsLabelLine(lLines[curLine])))
                    curLine++;
                else
                {
                    labelArray.Add(lLines[curLine].Substring(1, lLines[curLine].Length - 2));
                    curLine++;
                }
            }

            string sLine = "";

            for (int i = 0; i < lLines.Count; i++)
                {
                sLine = lLines[i];
                if (IsLabelLine(sLine))
                {
                    //record label in symbol table
                    //do not add the label line to the result
                    //if exists - need to cheack if give line number or not
                    String labelStr = sLine.Substring(1, sLine.Length - 2);//-2
                    if (sLine[1] < 58 && sLine[1] > 47)
                    {
                        throw new Exception("cant start with num");
                    }
                        if (!(labelDictionary.ContainsKey(labelStr)))
                        {
                            labelDictionary[labelStr] = counterOftabel;
                            counterOftabel++;
                        }
                        else
                        {
                           if (labelDictionary[sLine] ==(-1) )
                            {
                                labelDictionary[sLine] = counterOftabel;
                                counterOftabel++;
                            }
                        }
                    
            }
                else if (IsACommand(sLine))
                {
                    Boolean flag = false;
                    String pas = sLine.Substring(1);
                    flag = Int32.TryParse(pas, out numToParse);
                    if (flag == false)
                    {
                                if (sLine[1] < 58 && sLine[1] > 47)
                                { 
                                 throw new Exception("cant start with num");
                                }
                        if (labelArray.Contains(pas))
                        {
                            if (labelDictionary.ContainsKey(pas)) {
                            labelDictionary[sLine] = counterOftabel;
                            counterOftabel++;
                        }
                        else
                            labelDictionary[sLine] = (-1);
                        }
                     }
                }
                    //may contain a variable - if so, record it to the symbol table (if it doesn't exist there yet...)
                else if (IsCCommand(sLine))
                {
                    //do nothing here
                }
                else
                    throw new FormatException("Cannot parse line " + i + ": " + lLines[i]);
            }
          
        }
        
        //third pass - translate lines into machine code, replacing symbols with numbers
        private List<string> TranslateAssemblyToMachineCode(List<string> lLines)
        {
            string sLineSub =" "; int  numToParse;
            int[] control, jump, dest;
            string sLine = "";
            List<string> lAfterPass = new List<string>();
            for (int i = 0; i < lLines.Count; i++)
            {
                sLine = lLines[i];
                CleanWhiteSpacesAndComments(lLines[i]);
                if (IsACommand(sLine))
                {
                    sLineSub = sLine.Substring(1);
                    if (Int32.TryParse(sLineSub, out numToParse))
                        lAfterPass.Add(ToBinary(numToParse));
                    else
                    {
                        if (labelDictionary.ContainsKey(sLineSub))
                         lAfterPass.Add(ToBinary(labelDictionary[sLineSub]));
                    }

                    //translate an A command into a sequence of bits
                }
                else if (IsLabelLine(sLine))
                {
                    sLineSub = sLine.Substring(1, sLine.Length - 2);
                    if (!(labelDictionary.ContainsKey(sLineSub))) throw new Exception("no label");
                }
                else if (IsCCommand(sLine))
                {
                    string sDest, sControl, sJmp;
                    GetCommandParts(sLine, out sDest, out sControl, out sJmp);
                    //translate an C command into a sequence of bits
                    //take a look at the dictionaries m_dControl, m_dJmp, and where they are initialized (InitCommandDictionaries), to understand how to you them here
                    control = m_dControl[sControl];
                    jump = m_dJmp[sJmp];
                    dest = m_dest[sDest];
                    lAfterPass.Add("100" + ToString(control) + ToString(dest) + ToString(jump));
                }
                else
                    throw new FormatException("Cannot parse line " + i + ": " + lLines[i]);
            }
            labelDictionary.Clear();

            return lAfterPass;

        }

        //helper functions for translating numbers or bits into strings
        private string ToString(int[] aBits)
        {
            string sBinary = "";
            for (int i = 0; i < aBits.Length; i++)
                sBinary += aBits[i];
            return sBinary;
        }

        private string ToBinary(int x)
        {
            string sBinary = "";
            for (int i = 0; i < WORD_SIZE; i++)
            {
                sBinary = (x % 2) + sBinary;
                x = x / 2;
            }
            return sBinary;
        }


        //helper function for splitting the various fields of a C command
        private void GetCommandParts(string sLine, out string sDest, out string sControl, out string sJmp)
        {
            if (sLine.Contains('='))
            {
                int idx = sLine.IndexOf('=');
                sDest = sLine.Substring(0, idx);
                sLine = sLine.Substring(idx + 1);
            }
            else
                sDest = "";
            if (sLine.Contains(';'))
            {
                int idx = sLine.IndexOf(';');
                sControl = sLine.Substring(0, idx);
                sJmp = sLine.Substring(idx + 1);

            }
            else
            {
                sControl = sLine;
                sJmp = "";
            }
        }

        private bool IsCCommand(string sLine)
        {
            return !IsLabelLine(sLine) && sLine[0] != '@';
        }

        private bool IsACommand(string sLine)
        {
            return sLine[0] == '@';
        }

        private bool IsLabelLine(string sLine)
        {
            if (sLine.StartsWith("(") && sLine.EndsWith(")"))
                return true;
            return false;
        }

        private string CleanWhiteSpacesAndComments(string sDirty)
        {
            string sClean = "";
            for (int i = 0 ; i < sDirty.Length ; i++)
            {
                char c = sDirty[i];
                if (c == '/' && i < sDirty.Length - 1 && sDirty[i + 1] == '/') // this is a comment
                    return sClean;
                if (c > ' ' && c <= '~')//ignore white spaces
                    sClean += c;
            }
            return sClean;
        }


        private void InitCommandDictionaries()
        {
            m_dControl = new Dictionary<string, int[]>();

            m_dControl["0"] = new int[] { 0, 1, 0, 1, 0, 1, 0 };
            m_dControl["1"] = new int[] { 0, 1, 1, 1, 1, 1, 1 };
            m_dControl["-1"] = new int[] { 0, 1, 1, 1, 0, 1, 0 };
            m_dControl["D"] = new int[] { 0, 0, 0, 1, 1, 0, 0 };
            m_dControl["A"] = new int[] { 0, 1, 1, 0, 0, 0, 0 };
            m_dControl["!D"] = new int[] { 0, 0, 0, 1, 1, 0, 1 };
            m_dControl["!A"] = new int[] { 0, 1, 1, 0, 0, 0, 1 };
            m_dControl["-D"] = new int[] { 0, 0, 0, 1, 1, 1, 1 };
            m_dControl["-A"] = new int[] { 0, 1, 1, 0, 0,1, 1 };
            m_dControl["D+1"] = new int[] { 0, 0, 1, 1, 1, 1, 1 };
            m_dControl["A+1"] = new int[] { 0, 1, 1, 0, 1, 1, 1 };
            m_dControl["D-1"] = new int[] { 0, 0, 0, 1, 1, 1, 0 };
            m_dControl["A-1"] = new int[] { 0, 1, 1, 0, 0, 1, 0 };
            m_dControl["D+A"] = new int[] { 0, 0, 0, 0, 0, 1, 0 };
            m_dControl["D-A"] = new int[] { 0, 0, 1, 0, 0, 1, 1 };
            m_dControl["A-D"] = new int[] { 0, 0, 0, 0, 1,1, 1 };
            m_dControl["D&A"] = new int[] { 0, 0, 0, 0, 0, 0, 0 };
            m_dControl["D|A"] = new int[] { 0, 0, 1, 0,1, 0, 1 };

            m_dControl["M"] = new int[] { 1, 1, 1, 0, 0, 0, 0 };
            m_dControl["!M"] = new int[] { 1, 1, 1, 0, 0, 0, 1 };
            m_dControl["-M"] = new int[] { 1, 1, 1, 0, 0, 1, 1 };
            m_dControl["M+1"] = new int[] { 1, 1, 1, 0, 1, 1, 1 };
            m_dControl["M-1"] = new int[] { 1, 1, 1, 0, 0, 1, 0 };
            m_dControl["D+M"] = new int[] { 1, 0, 0, 0, 0, 1, 0 };
            m_dControl["D-M"] = new int[] { 1, 0, 1, 0, 0, 1, 1 };
            m_dControl["M-D"] = new int[] { 1, 0, 0, 0, 1, 1, 1 };
            m_dControl["D&M"] = new int[] { 1, 0, 0, 0, 0, 0, 0 };
            m_dControl["D|M"] = new int[] { 1, 0, 1, 0, 1, 0, 1 };


            m_dJmp = new Dictionary<string, int[]>();

            m_dJmp[""] = new int[] { 0, 0, 0 };
            m_dJmp["JGT"] = new int[] { 0, 0, 1 };
            m_dJmp["JEQ"] = new int[] { 0, 1, 0 };
            m_dJmp["JGE"] = new int[] { 0, 1, 1 };
            m_dJmp["JLT"] = new int[] { 1, 0, 0 };
            m_dJmp["JNE"] = new int[] { 1, 0, 1 };
            m_dJmp["JLE"] = new int[] { 1, 1, 0 };
            m_dJmp["JMP"] = new int[] { 1, 1, 1 };



            m_dest = new Dictionary<string, int[]>();
            m_dest[""] = new int[] {0,0,0};
            m_dest["M"] = new int[] {0,0,1};
            m_dest["D"] = new int[] {0,1,0};
            m_dest["MD"] = new int[] {0,1,1};
            m_dest["A"] = new int[] {1,0,0};
            m_dest["AM"] = new int[] {1,0,1};
            m_dest["AD"] = new int[] {1,1,0};
            m_dest["AMD"] = new int[] {1,1,1};

        }
    }

}
