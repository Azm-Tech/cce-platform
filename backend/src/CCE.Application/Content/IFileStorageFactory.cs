namespace CCE.Application.Content;

public interface IFileStorageFactory
{
    IFileStorage GetStorage(DownloadFileType fileType);
}
