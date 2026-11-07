
USE master;
GO

IF DB_ID(N'ThptA_FullDev') IS NULL
BEGIN
    EXEC(N'
        CREATE DATABASE [ThptA_FullDev]
        ON PRIMARY
        (
            NAME = N''ThptA_FullDev_Data'',
            FILENAME = N''G:\ThptA\Database\ThptA_FullDev.mdf'',
            SIZE = 16MB,
            FILEGROWTH = 16MB
        )
        LOG ON
        (
            NAME = N''ThptA_FullDev_Log'',
            FILENAME = N''G:\ThptA\Database\ThptA_FullDev_log.ldf'',
            SIZE = 8MB,
            FILEGROWTH = 8MB
        )
    ');
END
ELSE
BEGIN
    PRINT N'Database ThptA_FullDev đã tồn tại.';
END;
GO

SELECT name, physical_name
FROM sys.master_files
WHERE database_id = DB_ID(N'ThptA_FullDev');
GO
