using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mzinga.Engine;

namespace Mzinga.Tests.New
{
    [TestClass]
    public class EngineTests
    {
        private List<string> _consoleOutput = [];

        [TestInitialize]
        public void Setup()
        {
            _consoleOutput = [];
        }

        private void MockConsoleOut(string format, params object[] args)
        {
            _consoleOutput.Add(string.Format(format, args));
        }

        [TestMethod]
        public void Engine_Constructor()
        {
            var config = new EngineConfig();
            
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut, "Test License");

            Assert.AreEqual("TestEngine", engine.ID);
            Assert.AreEqual(config, engine.Config);
            Assert.AreEqual("Test License", engine.LicensesText);
            Assert.IsFalse(engine.ExitRequested);
        }

        [TestMethod]
        public void Engine_ParseCommand_Info()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine123", config, MockConsoleOut);

            engine.ParseCommand("info");

            Assert.IsNotEmpty(_consoleOutput);
            Assert.Contains("id TestEngine123", _consoleOutput[0]);
            Assert.Contains("ok", _consoleOutput[^1]);
        }

        [TestMethod]
        public void Engine_ParseCommand_Help()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("help");

            Assert.IsNotEmpty(_consoleOutput);

            bool foundNewGame = false;
            foreach(var line in _consoleOutput)
            {
                 if(line.Contains("newgame"))
                    foundNewGame = true;
            }
            Assert.IsTrue(foundNewGame);

            _consoleOutput.Clear();

            engine.ParseCommand("help info");
            Assert.Contains("  info", _consoleOutput[0]);

            _consoleOutput.Clear();

            engine.ParseCommand("help newgame");
            Assert.Contains("  newgame [GameTypeString|GameString]", _consoleOutput[0]);

            _consoleOutput.Clear();

            engine.ParseCommand("help options");
            Assert.Contains("  options get OptionName", _consoleOutput[1]);
            
            _consoleOutput.Clear();

            engine.ParseCommand("help play");
            bool foundPlayHelp = false;
            foreach (var line in _consoleOutput)
            {
                if (line.Contains("play MoveString"))
                {
                    foundPlayHelp = true;
                    break;
                }
            }
            Assert.IsTrue(foundPlayHelp);
            
            _consoleOutput.Clear();
            
            engine.ParseCommand("help InvalidCommand");
            Assert.Contains("err", _consoleOutput[0]);
        }

        [TestMethod]
        public void Engine_ParseCommand_NewGame()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("newgame");
            Assert.Contains("ok", _consoleOutput[^1]);

            _consoleOutput.Clear();

            engine.ParseCommand("validmoves");
            Assert.IsNotEmpty(_consoleOutput);
            Assert.Contains("ok", _consoleOutput[^1]);
        }

        [TestMethod]
        public void Engine_ParseCommand_Exit()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            Assert.IsFalse(engine.ExitRequested);
            engine.ParseCommand("exit");
            Assert.IsTrue(engine.ExitRequested);
        }

        [TestMethod]
        public void Engine_ParseCommand_Invalid()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("invalidcommand");
            
            Assert.Contains("err", _consoleOutput[0]);
        }

        [TestMethod]
        public void Engine_ParseCommand_NoBoardException()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("validmoves");

            bool hasNoBoardError = false;
            foreach(var o in _consoleOutput)
            {
                if(o.Contains("err No game in progress"))
                {
                    hasNoBoardError = true;
                    break;
                }
            }
            Assert.IsTrue(hasNoBoardError);
        }

        [TestMethod]
        public void Engine_ParseCommand_UndoInvalidNumberOfMovesException()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("newgame");
            _consoleOutput.Clear();
            
            engine.ParseCommand("undo 1");
            
            bool hasUndo1Error = false;
            foreach(var o in _consoleOutput)
            {
                if(o.Contains("err Unable to undo 1 moves"))
                {
                    hasUndo1Error = true;
                    break;
                }
            }
            Assert.IsTrue(hasUndo1Error);
            
            _consoleOutput.Clear();
            engine.ParseCommand("undo 0");
            
            bool hasUndo0Error = false;
            foreach(var o in _consoleOutput)
            {
                if(o.Contains("err Unable to undo 0 moves"))
                {
                    hasUndo0Error = true;
                    break;
                }
            }
            Assert.IsTrue(hasUndo0Error);
        }

        [TestMethod]
        public void Engine_ParseCommand_PerftInvalidDepthException()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("newgame");
            _consoleOutput.Clear();
            
            engine.ParseCommand("perft -1");
            
            bool hasDepthError = false;
            foreach(var o in _consoleOutput)
            {
                if(o.Contains("err Unable to calculate perft(-1)"))
                {
                    hasDepthError = true;
                    break;
                }
            }
            Assert.IsTrue(hasDepthError);
        }
        
        [TestMethod]
        public void Engine_ParseCommand_ArgumentNullException()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            bool exceptionThrown = false;
            try
            {
                engine.ParseCommand("   ");
            }
            catch(ArgumentNullException)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown);
        }

        [TestMethod]
        public void Engine_ParseCommand_Licences()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut, "My Custom License");

            engine.ParseCommand("licenses");

            Assert.Contains("My Custom License", _consoleOutput);
            Assert.Contains("ok", _consoleOutput[^1]);
        }

        [TestMethod]
        public void Engine_ParseCommand_Options()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("options");

            bool foundOptions = false;
            foreach (var line in _consoleOutput)
            {
                if (line.Contains("MaxBranchingFactor"))
                {
                    foundOptions = true;
                    break;
                }
            }
            Assert.IsTrue(foundOptions);
            Assert.Contains("ok", _consoleOutput[^1]);
        }

        [TestMethod]
        public void Engine_ParseCommand_OptionsGet()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("options get MaxBranchingFactor");

            bool foundOption = false;
            foreach (var line in _consoleOutput)
            {
                if (line.Contains(config.MaxBranchingFactor.ToString()!))
                {
                    foundOption = true;
                    break;
                }
            }
            Assert.IsTrue(foundOption);
            Assert.Contains("ok", _consoleOutput[^1]);
        }

        [TestMethod]
        public void Engine_ParseCommand_OptionsSet()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("options set ReportIntermediateBestMoves true");
            Assert.IsTrue(engine.Config.ReportIntermediateBestMoves);

            engine.ParseCommand("options set MaxBranchingFactor 100");
            Assert.AreEqual(100, engine.Config.MaxBranchingFactor);

            engine.ParseCommand("options set MaxHelperThreads 2");
            Assert.AreEqual(2, engine.Config.MaxHelperThreads);

            engine.ParseCommand("options set PonderDuringIdle MultiThreaded");
            Assert.AreEqual(PonderDuringIdleType.MultiThreaded, engine.Config.PonderDuringIdle);

            engine.ParseCommand("options set QuiescentSearchMaxDepth 12");
            Assert.AreEqual(12, engine.Config.QuiescentSearchMaxDepth);

            engine.ParseCommand("options set TranspositionTableSizeMB 64");
            Assert.AreEqual(64, engine.Config.TranspositionTableSizeMB);

            engine.ParseCommand("options set UseNullAspirationWindow false");
            Assert.IsFalse(engine.Config.UseNullAspirationWindow);

            engine.ParseCommand("newgame GameString;Base;NotStarted;White[1];Black[1]");
            engine.ParseCommand("options set MaxHelperThreads 1");
            engine.ParseCommand("options set PonderDuringIdle SingleThreaded");
            Assert.AreEqual(PonderDuringIdleType.SingleThreaded, engine.Config.PonderDuringIdle);

            //engine.ParseCommand("newgame");
        }

        [TestMethod]
        public void Engine_ParseCommand_Play_NoBoardWhenNoGame()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("play WA1");

            bool hasNoBoardError = false;
            foreach(var o in _consoleOutput)
            {
                if(o.Contains("err No game in progress"))
                {
                    hasNoBoardError = true;
                    break;
                }
            }
            Assert.IsTrue(hasNoBoardError);
        }

        [TestMethod]
        public void Engine_ParseCommand_PlayUndo()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("newgame");
            _consoleOutput.Clear();

            engine.ParseCommand("play WA1");
            Assert.Contains("ok", _consoleOutput[^1]);
            _consoleOutput.Clear();

            engine.ParseCommand("undo 1");
            Assert.Contains("ok", _consoleOutput[^1]);
        }

        [TestMethod]
        public void Engine_ParseCommand_Pass_NoBoardWhenNoGame()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("pass");

            bool hasNoBoardError = false;
            foreach(var o in _consoleOutput)
            {
                if(o.Contains("err No game in progress"))
                {
                    hasNoBoardError = true;
                    break;
                }
            }
            Assert.IsTrue(hasNoBoardError);
        }

        [TestMethod]
        public void Engine_ParseCommand_NewGameWithGameType()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("newgame Base+M+L+P");

            Assert.Contains("ok", _consoleOutput[^1]);
            _consoleOutput.Clear();

            engine.ParseCommand("validmoves");
            Assert.IsNotEmpty(_consoleOutput);
        }

        [TestMethod]
        public void Engine_ParseCommand_NewGameWithGameString()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("newgame Base;InProgress;White[1];White[1];1");

            Assert.Contains("ok", _consoleOutput[^1]);
            _consoleOutput.Clear();

            engine.ParseCommand("validmoves");
            Assert.IsNotEmpty(_consoleOutput);
        }

        [TestMethod]
        public void Engine_ParseCommand_BestMoveTime()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("newgame");
            _consoleOutput.Clear();

            engine.ParseCommand("bestmove time 00:00:01");
            Assert.Contains("ok", _consoleOutput[^1]);
        }

        [TestMethod]
        public void Engine_ParseCommand_PlayWithoutArgs()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("play");
            Assert.Contains("err", _consoleOutput[0]);
        }
        
        [TestMethod]
        public void Engine_ParseCommand_BestMoveWithInvalidArgs_CommandException()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("bestmove invalidarg");
            Assert.Contains("err", _consoleOutput[0]);
        }

        [TestMethod]
        public void Engine_ParseCommand_OptionsWithInvalidArgs_CommandException()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("options invalidarg");
            Assert.Contains("err", _consoleOutput[0]);
        }

        [TestMethod]
        public void Engine_ParseCommand_PerftWithArgs()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("newgame");
            _consoleOutput.Clear();

            engine.ParseCommand("perft 1");
            Assert.Contains("ok", _consoleOutput[^1]);
        }

        [TestMethod]
        public void Engine_ParseCommand_UndoWithoutArgs()
        {
            var config = new EngineConfig();
            var engine = new Engine.Engine("TestEngine", config, MockConsoleOut);

            engine.ParseCommand("newgame");
            engine.ParseCommand("play WA1");
            _consoleOutput.Clear();

            engine.ParseCommand("undo");
            Assert.Contains("ok", _consoleOutput[^1]);
        }
    }
}
