﻿// ReSharper disable UnusedParameter.Local
[TestClass]
public class Tests :
    VerifyBase
{
    // ReSharper disable once UnusedMember.Local
    static void DerivePathInfo()
    {
        #region DerivePathInfoMSTest

        DerivePathInfo(
            (sourceFile, projectDirectory, type, method) => new(
                directory: Path.Combine(projectDirectory, "Snapshots"),
                typeName: type.Name,
                methodName: method.Name));

        #endregion
    }

    [DataTestMethod]
    [DataRow("Value1")]
    public Task MissingParameter(string arg) =>
        Verify("Foo");

    [DataTestMethod]
    [DataRow("Value1")]
    public Task UseFileNameWithParam(string arg) =>
        Verify(arg)
            .UseFileName("UseFileNameWithParam");

    [DataTestMethod]
    [DataRow("Value1")]
    public Task UseTextForParameters(string arg) =>
        Verify(arg)
            .UseTextForParameters("TextForParameter");

    [TestMethod]
    public Task StringTarget() =>
        Verify(new Target("txt", "Value"));

    [ResultFilesCallback]
    [TestMethod]
    public async Task ChangeHasAttachment()
    {
        ResultFilesCallback.Callback = list =>
        {
            Assert.AreEqual(1, list.Count);
            var file = Path.GetFileName(list[0]);
            Assert.AreEqual($"Tests.ChangeHasAttachment.{Namer.TargetFrameworkNameAndVersion}.received.txt", file);
        };
        var settings = new VerifySettings();
        settings.DisableDiff();
        await Assert.ThrowsExceptionAsync<VerifyException>(
            () => Verify("Bar", settings));
    }
    [ResultFilesCallback]
    [TestMethod]
    public async Task AutoVerifyHasAttachment()
    {
        var path = CurrentFile.Relative("Tests.AutoVerifyHasAttachment.verified.txt");
        var fullPath = Path.GetFullPath(path);
        File.Delete(fullPath);
        File.WriteAllText(fullPath,"Foo");
        ResultFilesCallback.Callback = list =>
        {
            Assert.AreEqual(1, list.Count);
            var file = Path.GetFileName(list[0]);
            Assert.AreEqual("Tests.AutoVerifyHasAttachment.verified.txt", file);
        };
        var settings = new VerifySettings();
        settings.DisableDiff();
        settings.AutoVerify();
        await Verify("Bar", settings);
    }

    [ResultFilesCallback]
    [TestMethod]
    public async Task NewHasAttachment()
    {
        ResultFilesCallback.Callback = list =>
        {
            Assert.AreEqual(1, list.Count);
            var file = Path.GetFileName(list[0]);
            Assert.AreEqual($"Tests.NewHasAttachment.{Namer.TargetFrameworkNameAndVersion}.received.txt", file);
        };
        var settings = new VerifySettings();
        settings.DisableDiff();
        await Assert.ThrowsExceptionAsync<VerifyException>(
            () => Verify("Bar", settings));
    }

    [ResultFilesCallback]
    [TestMethod]
    public async Task MultipleChangedHasAttachment()
    {
        ResultFilesCallback.Callback = list =>
        {
            Assert.AreEqual(2, list.Count);
            var file0 = Path.GetFileName(list[0]);
            var file1 = Path.GetFileName(list[1]);
            Assert.AreEqual($"Tests.MultipleChangedHasAttachment.{Namer.TargetFrameworkNameAndVersion}#00.received.txt", file0);
            Assert.AreEqual($"Tests.MultipleChangedHasAttachment.{Namer.TargetFrameworkNameAndVersion}#01.received.txt", file1);
        };
        var settings = new VerifySettings();
        settings.DisableDiff();
        await Assert.ThrowsExceptionAsync<VerifyException>(
            () => Verify("Bar", [new Target("txt", "Value")], settings));
    }

    [ResultFilesCallback]
    [TestMethod]
    public async Task MultipleNewHasAttachment()
    {
        ResultFilesCallback.Callback = list =>
        {
            Assert.AreEqual(2, list.Count);
            var file0 = Path.GetFileName(list[0]);
            var file1 = Path.GetFileName(list[1]);
            Assert.AreEqual($"Tests.MultipleNewHasAttachment.{Namer.TargetFrameworkNameAndVersion}#00.received.txt", file0);
            Assert.AreEqual($"Tests.MultipleNewHasAttachment.{Namer.TargetFrameworkNameAndVersion}#01.received.txt", file1);
        };
        var settings = new VerifySettings();
        settings.DisableDiff();
        await Assert.ThrowsExceptionAsync<VerifyException>(
            () => Verify("Bar",[new Target("txt", "Value")], settings));
    }

    #region ExplicitTargetsMsTest

    [TestMethod]
    public Task WithTargets() =>
        Verify(
            target: new
            {
                Property = "Value"
            },
            rawTargets:
            [
                new(
                    extension: "txt",
                    data: "Raw target value",
                    name: "targetName")
            ]);

    #endregion

    [TestMethod]
    public Task EnumerableTargets() =>
        Verify(
            new[]
            {
                new Target(
                    extension: "txt",
                    data: "Raw target value",
                    name: "targetName")
            });

    static string directoryPathToVerify = Path.Combine(AttributeReader.GetSolutionDirectory(), "ToVerify");

    #region VerifyDirectoryMsTest

    [TestMethod]
    public Task WithDirectory() =>
        VerifyDirectory(directoryPathToVerify);

    #endregion

    static string zipPath = Path.Combine(AttributeReader.GetSolutionDirectory(), "ToVerify.zip");

    #region WithZipMsTest

    [TestMethod]
    public Task WithZip() =>
        VerifyZip(zipPath);

    #endregion
}