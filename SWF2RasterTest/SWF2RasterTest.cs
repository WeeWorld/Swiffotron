﻿//-----------------------------------------------------------------------
// SWF2RasterTest.cs
//
//
//-----------------------------------------------------------------------

namespace SWF2RasterTest
{
    using SWFProcessing.SWF2Raster;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using SWFProcessing.SWFModeller;
    using System.IO;
    using SWFProcessing.SWFModeller.Process;

    /// <summary>
    ///This is a test class for SWF2RasterTest and is intended
    ///to contain all SWF2RasterTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SWF2RasterTest
    {

        private string TestDir;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        /// Called before each test.
        /// </summary>
        [TestInitialize]
        public void InitilizeTests()
        {
            this.TestDir = this.TestContext.TestDir + @"\Out\" +
                    this.GetType().Name + @"." + this.TestContext.TestName + @"\";

            Directory.CreateDirectory(this.TestDir);
        }

        /// <summary>
        /// A test for the simplest of SWF files.
        /// </summary>
        [TestMethod()]
        public void ConvertSimplestSWF()
        {
            ConvertSWF("SWF2RasterTest.ConvertSimplestSWF");
        }

        public void ConvertSWF(string name)
        {
            SWF swf = new SWF(new SWFContext(name), false);

            SWF2Raster converter = new SWF2Raster(swf);

            using (FileStream output = new FileStream(TestDir + name + ".png", FileMode.Create))
            using (Stream svgOut = converter.GetPNG())
            {
                byte[] buffer = new byte[32768];
                while (true)
                {
                    int read = svgOut.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        return;
                    output.Write(buffer, 0, read);
                }
            }
        }
    }
}