-- SQL Server. Ejecutar sobre TicketsCore después de aplicar las migraciones EF.
-- Ejecutar con sqlcmd o SSMS: GO es un separador de lotes, no una sentencia T-SQL.
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- SP: consulta del historial de un ticket. El ER no define una fecha u orden temporal.
CREATE OR ALTER PROCEDURE [dbo].[SP_GetTicketHistory]
    @Ticket_id uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [ID], [Ticket_id], [User_id], [Comment], [prevState], [newState]
    FROM [dbo].[HISTORY]
    WHERE [Ticket_id] = @Ticket_id;
END;
GO

-- FN: número de tickets asociados a un estado.
CREATE OR ALTER FUNCTION [dbo].[FN_CountTicketsByState]
(
    @State_id uniqueidentifier
)
RETURNS bigint
AS
BEGIN
    RETURN (SELECT COUNT_BIG(*) FROM [dbo].[TICKET] WHERE [State_id] = @State_id);
END;
GO

-- TG: declaración de referencia pendiente de una regla de negocio.
-- No se instala un trigger vacío ni se atribuye un cambio al creador del ticket:
-- HISTORY.User_id debe identificar al usuario que realmente registra el evento.
-- Antes de activar un trigger, definir cómo obtener ese actor y Comment,
-- considerar INSERT/UPDATE de múltiples filas y configurar UseSqlOutputClause(false)
-- para la tabla afectada en EF Core SQL Server.
/*
CREATE OR ALTER TRIGGER [dbo].[TG_TICKET_AfterUpdate]
ON [dbo].[TICKET]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    -- Implementar la regla acordada usando las tablas inserted y deleted.
END;
GO
*/
