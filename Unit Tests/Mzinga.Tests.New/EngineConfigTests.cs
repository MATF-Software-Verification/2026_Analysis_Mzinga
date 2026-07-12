using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mzinga.Engine;
using Mzinga.Core.AI;
using Mzinga.Core;

namespace Mzinga.Tests.New
{
    [TestClass]
    public class EngineConfigTests
    {
        [TestMethod]
        public void EngineConfig_DefaultConstructor()
        {
            var config = new EngineConfig();

            Assert.IsNotNull(config.MetricWeightSet);
            Assert.AreEqual(EngineConfig.DefaultMaxHelperThreads, config.MaxHelperThreads);
            Assert.AreEqual(EngineConfig.DefaultReportIntermediateBestMoves, config.ReportIntermediateBestMoves);
            Assert.IsNull(config.MaxBranchingFactor);
        }

        [TestMethod]
        public void EngineConfig_LoadConfig_ValidXmlStream()
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
                        <Mzinga.Engine>
                            <GameAI>
                                <MaxBranchingFactor>100</MaxBranchingFactor>
                                <MaxHelperThreads>2</MaxHelperThreads>
                                <PonderDuringIdle>SingleThreaded</PonderDuringIdle>
                            </GameAI>
                        </Mzinga.Engine>";
            
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            var config = new EngineConfig(stream);

            Assert.AreEqual(100, config.MaxBranchingFactor);
            Assert.AreEqual(2, config.MaxHelperThreads);
            Assert.AreEqual(PonderDuringIdleType.SingleThreaded, config.PonderDuringIdle);
        }

        [TestMethod]
        public void EngineConfig_LoadConfig_InvalidXmlStream()
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
                        <Mzinga.Engine>
                            <GameAI>
                                <MaxBranchingFactor>100
                            </GameAI>
                        </Mzinga.Engine>"; // Missing closing tag
            
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            
            bool exceptionThrown = false;
            try
            {
                new EngineConfig(stream);
            }
            catch (System.Xml.XmlException)
            {
                exceptionThrown = true;
            }
            
            Assert.IsTrue(exceptionThrown);
        }

        [TestMethod]
        public void EngineConfig_ParseMaxHelperThreads()
        {
            var config = new EngineConfig();

            config.ParseMaxHelperThreadsValue("2");
            Assert.AreEqual(2, config.MaxHelperThreads);

            config.ParseMaxHelperThreadsValue("None");
            Assert.AreEqual(0, config.MaxHelperThreads);

            config.ParseMaxHelperThreadsValue("Auto");
            Assert.AreEqual(EngineConfig.DefaultMaxHelperThreads, config.MaxHelperThreads);
        }

        [TestMethod]
        public void EngineConfig_GetOptionsClone()
        {
            var config = new EngineConfig();
            config.LoadConfig(new MemoryStream(Encoding.UTF8.GetBytes(@"<?xml version=""1.0"" encoding=""utf-8""?>
                        <Mzinga.Engine>
                            <GameAI>
                                <MaxBranchingFactor>50</MaxBranchingFactor>
                                <MaxHelperThreads>3</MaxHelperThreads>
                                <PonderDuringIdle>MultiThreaded</PonderDuringIdle>
                                <ReportIntermediateBestMoves>True</ReportIntermediateBestMoves>
                            </GameAI>
                        </Mzinga.Engine>")));

            var clone = config.GetOptionsClone();

            Assert.AreEqual(50, clone.MaxBranchingFactor);
            Assert.AreEqual(3, clone.MaxHelperThreads);
            Assert.AreEqual(PonderDuringIdleType.MultiThreaded, clone.PonderDuringIdle);
            Assert.IsTrue(clone.ReportIntermediateBestMoves);
        }

        [TestMethod]
        public void EngineConfig_CopyOptionsFrom()
        {
            var config = new EngineConfig();
            config.LoadConfig(new MemoryStream(Encoding.UTF8.GetBytes(@"<?xml version=""1.0"" encoding=""utf-8""?>
                        <Mzinga.Engine>
                            <GameAI>
                                <MaxBranchingFactor>75</MaxBranchingFactor>
                                <MaxHelperThreads>1</MaxHelperThreads>
                                <PonderDuringIdle>SingleThreaded</PonderDuringIdle>
                                <TranspositionTableSizeMB>64</TranspositionTableSizeMB>
                            </GameAI>
                        </Mzinga.Engine>")));

            var configCopy = new EngineConfig();
            configCopy.CopyOptionsFrom(config);

            Assert.AreEqual(75, configCopy.MaxBranchingFactor);
            Assert.AreEqual(1, configCopy.MaxHelperThreads);
            Assert.AreEqual(PonderDuringIdleType.SingleThreaded, configCopy.PonderDuringIdle);
            Assert.AreEqual(64, configCopy.TranspositionTableSizeMB);
        }

        [TestMethod]
        public void EngineConfig_SaveConfig()
        {
            var config = new EngineConfig();
            config.LoadConfig(new MemoryStream(Encoding.UTF8.GetBytes(@"<?xml version=""1.0"" encoding=""utf-8""?>
                        <Mzinga.Engine>
                            <GameAI>
                                <MaxBranchingFactor>20</MaxBranchingFactor>
                                <MaxHelperThreads>Auto</MaxHelperThreads>
                                <PonderDuringIdle>MultiThreaded</PonderDuringIdle>
                            </GameAI>
                        </Mzinga.Engine>")));
            
            using var stream = new MemoryStream();
            config.SaveConfig(stream, "TestRoot", ConfigSaveType.BasicOptions);
            
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            string savedXml = reader.ReadToEnd();

            Assert.Contains("<MaxBranchingFactor>20</MaxBranchingFactor>", savedXml);
            Assert.Contains("<MaxHelperThreads>Auto</MaxHelperThreads>", savedXml);
            Assert.Contains("<PonderDuringIdle>MultiThreaded</PonderDuringIdle>", savedXml);
        }
    }
}
