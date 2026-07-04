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
        public void DefaultConstructor_InitializeCorrectly()
        {
            var config = new EngineConfig();

            Assert.IsNotNull(config.MetricWeightSet);
            Assert.AreEqual(EngineConfig.DefaultMaxHelperThreads, config.MaxHelperThreads);
            Assert.AreEqual(EngineConfig.DefaultReportIntermediateBestMoves, config.ReportIntermediateBestMoves);
            Assert.IsNull(config.MaxBranchingFactor);
        }

        [TestMethod]
        public void LoadConfig_WithValidXmlStream_LoadsOptions()
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
        public void LoadConfig_WithInvalidXmlStream_ThrowsXmlException()
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
                        <Mzinga.Engine>
                            <GameAI>
                                <MaxBranchingFactor>100
                            </GameAI>
                        </Mzinga.Engine>"; // Missing closing tag for MaxBranchingFactor
            
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
    }
}
