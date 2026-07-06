using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mzinga;
using static System.Net.Mime.MediaTypeNames;

namespace Mzinga.Tests.New
{
    [TestClass]
    public class AppInfoTests
    {
        [TestMethod]
        public void AppInfo_Properties()
        {
            Assert.IsNotNull(AppInfo.Name);
            Assert.IsNotNull(AppInfo.Version);
            Assert.IsGreaterThanOrEqualTo(0.0, AppInfo.LongVersion);
            Assert.IsNotNull(AppInfo.Product);
            Assert.IsNotNull(AppInfo.Copyright);

            Assert.IsNotNull(AppInfo.FormattedLicensesText);
            Assert.IsNotNull(AppInfo.HiveProduct);
            Assert.IsNotNull(AppInfo.HiveCopyright);
            Assert.IsNotNull(AppInfo.HiveLicense);
            Assert.IsNotNull(AppInfo.MitLicenseName);
            Assert.IsNotNull(AppInfo.MitLicenseBody);

            Assert.Contains(AppInfo.HiveProduct, AppInfo.FormattedLicensesText);
            Assert.Contains(AppInfo.HiveLicense, AppInfo.FormattedLicensesText);
            Assert.Contains(AppInfo.MitLicenseName, AppInfo.FormattedLicensesText);
            Assert.Contains("Copyright", AppInfo.FormattedLicensesText);

            Assert.IsNotNull(AppInfo.EntryAssemblyPath);
        }
    }
}
