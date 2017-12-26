﻿using ColorzCore.DataTypes;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;
using ColorzCore.Raws;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ColorzCore
{
    //Class to excapsulate all steps in EA script interpretation.
    class EAInterpreter
    {
        private Dictionary<string, IList<Raw>> allRaws;
        private EAParser myParser;
        private string game, iFile;
        private Stream sin;
        private FileStream fout;
        private TextWriter serr;

        public EAInterpreter(string game, string rawsFolder, string rawsExtension, Stream sin, string inFileName, FileStream fout, TextWriter serr)
        {
            this.game = game;
            allRaws = ProcessRaws(game, LoadAllRaws(rawsFolder, rawsExtension));
            this.sin = sin;
            this.fout = fout;
            this.serr = serr;
            iFile = inFileName;
        }

        public void Interpret()
        {
            myParser = new EAParser(allRaws);
            myParser.Definitions['_' + game + '_'] = new Definition();
            
            Tokenizer t = new Tokenizer();
            ROM myROM = new ROM(fout);

            IList<ILineNode> lines = new List<ILineNode>(myParser.ParseAll(t.Tokenize(new BufferedStream(sin), iFile)));
            

            //TODO: sort them by file/line
            foreach (string message in myParser.Messages)
            {
                serr.WriteLine("MESSAGE: " + message);
            }
            foreach (string warning in myParser.Warnings)
            {
                serr.WriteLine("WARNING: " + warning);
            }
            foreach (string error in myParser.Errors)
            {
                serr.WriteLine("ERROR: " + error);
            }

            //TODO: -WError flag?
            if(myParser.Errors.Count == 0)
            {
                foreach(ILineNode line in lines)
                {
                    line.WriteData(myROM);
                }
                myROM.WriteROM();
            }
            else
            {
                serr.WriteLine("Errors occurred; no changes written.");
            }
        }

        private static IList<Raw> LoadAllRaws(string rawsFolder, string rawsExtension)
        {
            string folder;
            DirectoryInfo directoryInfo = new DirectoryInfo(rawsFolder);
            folder = Path.GetFullPath(rawsFolder);
            FileInfo[] files = directoryInfo.GetFiles("*" + rawsExtension, SearchOption.AllDirectories);
            IEnumerable<Raw> allRaws = new List<Raw>();
            foreach (FileInfo fileInfo in files)
            {
                FileStream fs = new FileStream(fileInfo.FullName, FileMode.Open);
                allRaws = allRaws.Concat(Raw.ParseAllRaws(fs));
                fs.Close();
            }
            return new List<Raw>(allRaws);
        }
        private static Dictionary<string, IList<Raw>> ProcessRaws(string game, IList<Raw> allRaws)
        {
            Dictionary<string, IList<Raw>> retVal = new Dictionary<string, IList<Raw>>();
            foreach (Raw r in allRaws)
            {
                if (r.Game.Contains(game))
                    retVal.AddTo(r.Name, r);
            }
            return retVal;
        }
    }
}