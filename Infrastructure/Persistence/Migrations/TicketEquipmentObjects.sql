-- NULL conserva el campo actual. Las ediciones sin cambios se rechazan.
CREATE OR ALTER PROCEDURE dbo.SP_EditTicket
    @Ticket_id uniqueidentifier,
    @User_id uniqueidentifier,
    @Title nvarchar(max) = NULL,
    @Description nvarchar(max) = NULL,
    @Comment nvarchar(max) = NULL,
    @Equipment nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @StateId uniqueidentifier, @State nvarchar(256), @Now datetime2 = SYSUTCDATETIME();
        SELECT @StateId = t.State_id, @State = s.name
        FROM dbo.TICKET t WITH (UPDLOCK, HOLDLOCK)
        JOIN dbo.STATE s ON s.ID = t.State_id
        WHERE t.ID = @Ticket_id AND t.DeletedAt IS NULL;
        IF @StateId IS NULL THROW 51000, N'Ticket inexistente o eliminado.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers WHERE Id = @User_id)
            THROW 51001, N'El actor no existe.', 1;
        IF (@Title IS NOT NULL AND LEN(LTRIM(RTRIM(@Title))) = 0)
            OR (@Description IS NOT NULL AND LEN(LTRIM(RTRIM(@Description))) = 0)
            OR (@Equipment IS NOT NULL AND LEN(LTRIM(RTRIM(@Equipment))) = 0)
            THROW 51004, N'El título, la descripción y el equipo no pueden quedar vacíos.', 1;
        DECLARE @OldTitle nvarchar(max), @OldDescription nvarchar(max), @OldEquipment nvarchar(max),
            @Changes nvarchar(max) = N'';
        SELECT @OldTitle = Title, @OldDescription = Description, @OldEquipment = Equipment
        FROM dbo.TICKET WHERE ID = @Ticket_id;
        IF @Title IS NOT NULL AND CONVERT(varbinary(max), @Title) <> CONVERT(varbinary(max), @OldTitle)
            SET @Changes = CONCAT(@Changes, N' | Título: [', @OldTitle, N'] -> [', @Title, N']');
        IF @Description IS NOT NULL AND CONVERT(varbinary(max), @Description) <> CONVERT(varbinary(max), @OldDescription)
            SET @Changes = CONCAT(@Changes, N' | Descripción: [', @OldDescription, N'] -> [', @Description, N']');
        IF @Equipment IS NOT NULL AND CONVERT(varbinary(max), @Equipment) <> CONVERT(varbinary(max), @OldEquipment)
            SET @Changes = CONCAT(@Changes, N' | Equipo: [', @OldEquipment, N'] -> [', @Equipment, N']');
        IF @Changes = N'' THROW 51005, N'No se proporcionaron cambios.', 1;
        UPDATE dbo.TICKET SET Title = COALESCE(@Title, Title),
            Description = COALESCE(@Description, Description), Equipment = COALESCE(@Equipment, Equipment),
            UpdatedAt = @Now WHERE ID = @Ticket_id;
        INSERT dbo.HISTORY (ID, Ticket_id, User_id, Comment, prevState, newState, CreatedAt, UpdatedAt)
        VALUES (NEWID(), @Ticket_id, @User_id, CONCAT(COALESCE(@Comment, N''), @Changes),
            @State, @State, @Now, @Now);
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- Un grupo por estado (incluye estados sin tickets); fechas más recientes primero.
CREATE OR ALTER PROCEDURE dbo.SP_GetTicketsGroupedByState
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.ID AS StateId, s.name AS StateName,
        COALESCE((SELECT t.ID, t.User_id, t.State_id, t.Title, t.Description, t.Equipment, t.CreatedAt, t.UpdatedAt
            FROM dbo.TICKET t
            WHERE t.State_id = s.ID AND t.DeletedAt IS NULL
            ORDER BY t.CreatedAt DESC, t.ID
            FOR JSON PATH), N'[]') AS Tickets
    FROM dbo.STATE s
    ORDER BY CASE s.name WHEN N'Pendiente' THEN 1 WHEN N'En Proceso' THEN 2
        WHEN N'Resuelto' THEN 3 ELSE 4 END, s.name;
END;
GO
