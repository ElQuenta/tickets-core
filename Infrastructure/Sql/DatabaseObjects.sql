SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- El actor se recibe del usuario autenticado, no del creador del ticket.
CREATE OR ALTER PROCEDURE dbo.SP_ChangeTicketState
    @Ticket_id uniqueidentifier,
    @State_id uniqueidentifier,
    @User_id uniqueidentifier,
    @Comment nvarchar(max) = NULL
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
        DECLARE @NewState nvarchar(256);
        SELECT @NewState = name FROM dbo.STATE WHERE ID = @State_id;
        IF @NewState IS NULL THROW 51002, N'Estado inexistente.', 1;
        IF NOT ((@State = N'Pendiente' AND @NewState = N'En Proceso')
             OR (@State = N'En Proceso' AND @NewState IN (N'Pendiente', N'Resuelto'))
             OR (@State = N'Resuelto' AND @NewState IN (N'Pendiente', N'En Proceso')))
            THROW 51003, N'Transición inválida. Avance un paso o retroceda a un estado anterior; no repita el estado.', 1;
        UPDATE dbo.TICKET SET State_id = @State_id, UpdatedAt = @Now WHERE ID = @Ticket_id;
        INSERT dbo.HISTORY (ID, Ticket_id, User_id, Comment, prevState, newState, CreatedAt, UpdatedAt)
        VALUES (NEWID(), @Ticket_id, @User_id,
            CONCAT(COALESCE(@Comment, N''), N' | Estado: ', @State, N' -> ', @NewState),
            @State, @NewState, @Now, @Now);
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

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

CREATE OR ALTER PROCEDURE dbo.SP_DeleteTicket
    @Ticket_id uniqueidentifier,
    @User_id uniqueidentifier,
    @Comment nvarchar(max) = NULL
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
        UPDATE dbo.TICKET SET DeletedAt = @Now, UpdatedAt = @Now WHERE ID = @Ticket_id;
        INSERT dbo.HISTORY (ID, Ticket_id, User_id, Comment, prevState, newState, CreatedAt, UpdatedAt)
        VALUES (NEWID(), @Ticket_id, @User_id,
            CONCAT(COALESCE(@Comment, N''), N' | Ticket eliminado (soft delete).'),
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

CREATE OR ALTER TRIGGER dbo.[TG_TICKET_Timestamps]
ON dbo.[TICKET]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(OBJECT_ID(N'dbo.TG_TICKET_Timestamps'), 'AFTER', 'DML') > 1 RETURN;
    UPDATE t SET CreatedAt = d.CreatedAt, UpdatedAt = SYSUTCDATETIME()
    FROM dbo.[TICKET] t
    JOIN inserted i ON t.[ID] = i.[ID]
    JOIN deleted d ON d.[ID] = i.[ID];
END;
GO

CREATE OR ALTER TRIGGER dbo.[TG_HISTORY_Timestamps]
ON dbo.[HISTORY]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(OBJECT_ID(N'dbo.TG_HISTORY_Timestamps'), 'AFTER', 'DML') > 1 RETURN;
    UPDATE t SET CreatedAt = d.CreatedAt, UpdatedAt = SYSUTCDATETIME()
    FROM dbo.[HISTORY] t
    JOIN inserted i ON t.[ID] = i.[ID]
    JOIN deleted d ON d.[ID] = i.[ID];
END;
GO

CREATE OR ALTER TRIGGER dbo.[TG_STATE_Timestamps]
ON dbo.[STATE]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(OBJECT_ID(N'dbo.TG_STATE_Timestamps'), 'AFTER', 'DML') > 1 RETURN;
    UPDATE t SET CreatedAt = d.CreatedAt, UpdatedAt = SYSUTCDATETIME()
    FROM dbo.[STATE] t
    JOIN inserted i ON t.[ID] = i.[ID]
    JOIN deleted d ON d.[ID] = i.[ID];
END;
GO

CREATE OR ALTER TRIGGER dbo.[TG_AspNetUsers_Timestamps]
ON dbo.[AspNetUsers]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(OBJECT_ID(N'dbo.TG_AspNetUsers_Timestamps'), 'AFTER', 'DML') > 1 RETURN;
    UPDATE t SET CreatedAt = d.CreatedAt, UpdatedAt = SYSUTCDATETIME()
    FROM dbo.[AspNetUsers] t
    JOIN inserted i ON t.[Id] = i.[Id]
    JOIN deleted d ON d.[Id] = i.[Id];
END;
GO

CREATE OR ALTER TRIGGER dbo.[TG_AspNetRoles_Timestamps]
ON dbo.[AspNetRoles]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(OBJECT_ID(N'dbo.TG_AspNetRoles_Timestamps'), 'AFTER', 'DML') > 1 RETURN;
    UPDATE t SET CreatedAt = d.CreatedAt, UpdatedAt = SYSUTCDATETIME()
    FROM dbo.[AspNetRoles] t
    JOIN inserted i ON t.[Id] = i.[Id]
    JOIN deleted d ON d.[Id] = i.[Id];
END;
GO

CREATE OR ALTER TRIGGER dbo.[TG_AspNetUserRoles_Timestamps]
ON dbo.[AspNetUserRoles]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(OBJECT_ID(N'dbo.TG_AspNetUserRoles_Timestamps'), 'AFTER', 'DML') > 1 RETURN;
    UPDATE t SET CreatedAt = d.CreatedAt, UpdatedAt = SYSUTCDATETIME()
    FROM dbo.[AspNetUserRoles] t
    JOIN inserted i ON t.[UserId] = i.[UserId] AND t.[RoleId] = i.[RoleId]
    JOIN deleted d ON d.[UserId] = i.[UserId] AND d.[RoleId] = i.[RoleId];
END;
GO

CREATE OR ALTER TRIGGER dbo.[TG_AspNetUserClaims_Timestamps]
ON dbo.[AspNetUserClaims]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(OBJECT_ID(N'dbo.TG_AspNetUserClaims_Timestamps'), 'AFTER', 'DML') > 1 RETURN;
    UPDATE t SET CreatedAt = d.CreatedAt, UpdatedAt = SYSUTCDATETIME()
    FROM dbo.[AspNetUserClaims] t
    JOIN inserted i ON t.[Id] = i.[Id]
    JOIN deleted d ON d.[Id] = i.[Id];
END;
GO

CREATE OR ALTER TRIGGER dbo.[TG_AspNetUserLogins_Timestamps]
ON dbo.[AspNetUserLogins]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(OBJECT_ID(N'dbo.TG_AspNetUserLogins_Timestamps'), 'AFTER', 'DML') > 1 RETURN;
    UPDATE t SET CreatedAt = d.CreatedAt, UpdatedAt = SYSUTCDATETIME()
    FROM dbo.[AspNetUserLogins] t
    JOIN inserted i ON t.[LoginProvider] = i.[LoginProvider] AND t.[ProviderKey] = i.[ProviderKey]
    JOIN deleted d ON d.[LoginProvider] = i.[LoginProvider] AND d.[ProviderKey] = i.[ProviderKey];
END;
GO

CREATE OR ALTER TRIGGER dbo.[TG_AspNetUserTokens_Timestamps]
ON dbo.[AspNetUserTokens]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(OBJECT_ID(N'dbo.TG_AspNetUserTokens_Timestamps'), 'AFTER', 'DML') > 1 RETURN;
    UPDATE t SET CreatedAt = d.CreatedAt, UpdatedAt = SYSUTCDATETIME()
    FROM dbo.[AspNetUserTokens] t
    JOIN inserted i ON t.[UserId] = i.[UserId] AND t.[LoginProvider] = i.[LoginProvider] AND t.[Name] = i.[Name]
    JOIN deleted d ON d.[UserId] = i.[UserId] AND d.[LoginProvider] = i.[LoginProvider] AND d.[Name] = i.[Name];
END;
GO

CREATE OR ALTER TRIGGER dbo.[TG_AspNetRoleClaims_Timestamps]
ON dbo.[AspNetRoleClaims]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF TRIGGER_NESTLEVEL(OBJECT_ID(N'dbo.TG_AspNetRoleClaims_Timestamps'), 'AFTER', 'DML') > 1 RETURN;
    UPDATE t SET CreatedAt = d.CreatedAt, UpdatedAt = SYSUTCDATETIME()
    FROM dbo.[AspNetRoleClaims] t
    JOIN inserted i ON t.[Id] = i.[Id]
    JOIN deleted d ON d.[Id] = i.[Id];
END;
GO
