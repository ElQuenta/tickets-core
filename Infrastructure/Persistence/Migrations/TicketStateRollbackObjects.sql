-- Avances de un paso; retrocesos a cualquier estado anterior.
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
