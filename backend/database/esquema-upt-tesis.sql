/* ============================================================================
   PASO 1 — Diseño de Base de Datos
   Repositorio de Tesis de Posgrado UPT
   Motor: SQL Server (compatible con LocalDB y SQL Server 2019+)
   ----------------------------------------------------------------
   Este script es la proyección T-SQL del modelo EF Core ya implementado
   (migración 20260908155943_Inicial). Se puede ejecutar varias veces sin
   errores (todas las sentencias están protegidas con IF NOT EXISTS).
   Los datos de usuarios/demos NO se siembran aquí: se insertan en el
   arranque de la API (Program.cs / Program.Semillas.cs) porque sus
   contraseñas viajan como hash (ASP.NET Core Identity).
   ============================================================================ */

USE [master];
IF DB_ID(N'TesisUPT') IS NULL
BEGIN
    CREATE DATABASE [TesisUPT];
END;
GO

ALTER DATABASE [TesisUPT] SET RECOVERY SIMPLE;
GO

USE [TesisUPT];
GO

/* ----------------------------------------------------------------------------
   TABLAS
   ---------------------------------------------------------------------------- */

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'TiposArticulo')
BEGIN
    CREATE TABLE [dbo].[TiposArticulo] (
        [IdTipoArticulo] INT            IDENTITY(1,1) NOT NULL,
        [Nombre]         NVARCHAR(100)  NOT NULL,
        [Descripcion]    NVARCHAR(500)  NULL,
        [Activo]         BIT            NOT NULL CONSTRAINT [DF_TiposArticulo_Activo] DEFAULT (1),
        [FechaReg]       DATETIME2      NOT NULL CONSTRAINT [DF_TiposArticulo_FechaReg] DEFAULT (SYSUTCDATETIME()),
        [FechaMod]       DATETIME2      NOT NULL CONSTRAINT [DF_TiposArticulo_FechaMod] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_TiposArticulo] PRIMARY KEY ([IdTipoArticulo])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Posgrados')
BEGIN
    CREATE TABLE [dbo].[Posgrados] (
        [IdPosgrado] INT            IDENTITY(1,1) NOT NULL,
        [Nivel]      INT            NOT NULL,
        [Nombre]     NVARCHAR(150)  NOT NULL,
        [Activo]     BIT            NOT NULL CONSTRAINT [DF_Posgrados_Activo] DEFAULT (1),
        [FechaReg]   DATETIME2      NOT NULL CONSTRAINT [DF_Posgrados_FechaReg] DEFAULT (SYSUTCDATETIME()),
        [FechaMod]   DATETIME2      NOT NULL CONSTRAINT [DF_Posgrados_FechaMod] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Posgrados] PRIMARY KEY ([IdPosgrado]),
        CONSTRAINT [UQ_Posgrados_Nombre_Nivel] UNIQUE ([Nombre], [Nivel])
    );
END;
GO


IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AspNetRoles')
BEGIN
    CREATE TABLE [dbo].[AspNetRoles] (
        [Id]               INT            IDENTITY(1,1) NOT NULL,
        [FechaReg]         DATETIME2      NOT NULL CONSTRAINT [DF_AspNetRoles_FechaReg] DEFAULT (SYSUTCDATETIME()),
        [FechaMod]         DATETIME2      NOT NULL CONSTRAINT [DF_AspNetRoles_FechaMod] DEFAULT (SYSUTCDATETIME()),
        [Name]             NVARCHAR(256)  NULL,
        [NormalizedName]   NVARCHAR(256)  NULL,
        [ConcurrencyStamp] NVARCHAR(MAX)  NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AspNetUsers')
BEGIN
    CREATE TABLE [dbo].[AspNetUsers] (
        [Id]                   INT                IDENTITY(1,1) NOT NULL,
        [FechaReg]             DATETIME2          NOT NULL CONSTRAINT [DF_AspNetUsers_FechaReg] DEFAULT (SYSUTCDATETIME()),
        [FechaMod]             DATETIME2          NOT NULL CONSTRAINT [DF_AspNetUsers_FechaMod] DEFAULT (SYSUTCDATETIME()),
        [UserName]             NVARCHAR(256)      NULL,
        [NormalizedUserName]   NVARCHAR(256)      NULL,
        [Email]                NVARCHAR(256)      NULL,
        [NormalizedEmail]      NVARCHAR(256)      NULL,
        [EmailConfirmed]       BIT                NOT NULL CONSTRAINT [DF_AspNetUsers_EmailConfirmed] DEFAULT (0),
        [PasswordHash]         NVARCHAR(MAX)      NULL,
        [SecurityStamp]        NVARCHAR(MAX)      NULL,
        [ConcurrencyStamp]     NVARCHAR(MAX)      NULL,
        [PhoneNumber]          NVARCHAR(MAX)      NULL,
        [PhoneNumberConfirmed] BIT                NOT NULL CONSTRAINT [DF_AspNetUsers_PhoneConfirmed] DEFAULT (0),
        [TwoFactorEnabled]     BIT                NOT NULL CONSTRAINT [DF_AspNetUsers_TwoFactor] DEFAULT (0),
        [LockoutEnd]           DATETIMEOFFSET     NULL,
        [LockoutEnabled]       BIT                NOT NULL CONSTRAINT [DF_AspNetUsers_LockoutEnabled] DEFAULT (0),
        [AccessFailedCount]    INT                NOT NULL CONSTRAINT [DF_AspNetUsers_AccessFailed] DEFAULT (0),
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AspNetRoleClaims')
BEGIN
    CREATE TABLE [dbo].[AspNetRoleClaims] (
        [Id]         INT           IDENTITY(1,1) NOT NULL,
        [RoleId]     INT           NOT NULL,
        [ClaimType]  NVARCHAR(MAX) NULL,
        [ClaimValue] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AspNetUserClaims')
BEGIN
    CREATE TABLE [dbo].[AspNetUserClaims] (
        [Id]         INT           IDENTITY(1,1) NOT NULL,
        [UserId]     INT           NOT NULL,
        [ClaimType]  NVARCHAR(MAX) NULL,
        [ClaimValue] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AspNetUserLogins')
BEGIN
    CREATE TABLE [dbo].[AspNetUserLogins] (
        [LoginProvider]        NVARCHAR(450) NOT NULL,
        [ProviderKey]          NVARCHAR(450) NOT NULL,
        [ProviderDisplayName]  NVARCHAR(MAX) NULL,
        [UserId]               INT           NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AspNetUserRoles')
BEGIN
    CREATE TABLE [dbo].[AspNetUserRoles] (
        [UserId]     INT          NOT NULL,
        [RoleId]     INT          NOT NULL,
        [FechaReg]   DATETIME2    NOT NULL CONSTRAINT [DF_AspNetUserRoles_FechaReg] DEFAULT (SYSUTCDATETIME()),
        [FechaMod]   DATETIME2    NOT NULL CONSTRAINT [DF_AspNetUserRoles_FechaMod] DEFAULT (SYSUTCDATETIME()),
        [UsuarioId]  INT          NULL,
        [RolId]      INT          NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AspNetUserTokens')
BEGIN
    CREATE TABLE [dbo].[AspNetUserTokens] (
        [UserId]        INT           NOT NULL,
        [LoginProvider] NVARCHAR(450) NOT NULL,
        [Name]          NVARCHAR(450) NOT NULL,
        [Value]         NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Alumnos')
BEGIN
    CREATE TABLE [dbo].[Alumnos] (
        [IdAlumno]      INT            IDENTITY(1,1) NOT NULL,
        [IdUsuario]     INT            NOT NULL,
        [Nombre]        NVARCHAR(150)  NOT NULL,
        [NumeroControl] NVARCHAR(30)   NOT NULL,
        [Correo]        NVARCHAR(320)  NOT NULL,
        [FechaReg]      DATETIME2      NOT NULL CONSTRAINT [DF_Alumnos_FechaReg] DEFAULT (SYSUTCDATETIME()),
        [FechaMod]      DATETIME2      NOT NULL CONSTRAINT [DF_Alumnos_FechaMod] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Alumnos] PRIMARY KEY ([IdAlumno])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Profesores')
BEGIN
    CREATE TABLE [dbo].[Profesores] (
        [IdProfesor] INT            IDENTITY(1,1) NOT NULL,
        [IdUsuario]  INT            NULL,
        [Nombre]     NVARCHAR(150)  NOT NULL,
        [Activo]     BIT            NOT NULL CONSTRAINT [DF_Profesores_Activo] DEFAULT (1),
        [FechaReg]   DATETIME2      NOT NULL CONSTRAINT [DF_Profesores_FechaReg] DEFAULT (SYSUTCDATETIME()),
        [FechaMod]   DATETIME2      NOT NULL CONSTRAINT [DF_Profesores_FechaMod] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Profesores] PRIMARY KEY ([IdProfesor])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ProfesorPosgrado')
BEGIN
    CREATE TABLE [dbo].[ProfesorPosgrado] (
        [IdProfesorPosgrado] INT           IDENTITY(1,1) NOT NULL,
        [IdProfesor]         INT           NOT NULL,
        [Nivel]              INT           NOT NULL,   -- 0 = Maestría, 1 = Doctorado
        [Programa]           NVARCHAR(150) NOT NULL,   -- aplica a ambos niveles (programa único)
        [FechaReg]           DATETIME2     NOT NULL CONSTRAINT [DF_ProfesorPosgrado_FechaReg] DEFAULT (SYSUTCDATETIME()),
        [FechaMod]           DATETIME2     NOT NULL CONSTRAINT [DF_ProfesorPosgrado_FechaMod] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ProfesorPosgrado] PRIMARY KEY ([IdProfesorPosgrado])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Tesis')
BEGIN
    CREATE TABLE [dbo].[Tesis] (
        [Folio]             UNIQUEIDENTIFIER NOT NULL,
        [IdAlumno]          INT              NOT NULL,
        [Titulo]            NVARCHAR(300)    NOT NULL,
        [Nivel]             INT              NOT NULL,   -- 0 = Maestría, 1 = Doctorado
        [Programa]          NVARCHAR(150)    NOT NULL,
        [IdAsesor]          INT              NOT NULL,
        [TieneCoAsesor]     BIT              NOT NULL CONSTRAINT [DF_Tesis_TieneCoAsesor] DEFAULT (0),
        [IdCoAsesor]        INT              NULL,
        [Version]           INT              NOT NULL CONSTRAINT [DF_Tesis_Version] DEFAULT (0),
        [Estado]            INT              NOT NULL,   -- ver CHECK + tabla Estados abajo
        [FechaExamenGrado]  DATETIME2        NULL,
        [FechaReg]          DATETIME2        NOT NULL CONSTRAINT [DF_Tesis_FechaReg] DEFAULT (SYSUTCDATETIME()),
        [FechaMod]          DATETIME2        NOT NULL CONSTRAINT [DF_Tesis_FechaMod] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Tesis] PRIMARY KEY ([Folio])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'DocumentosTesis')
BEGIN
    CREATE TABLE [dbo].[DocumentosTesis] (
        [IdDocumento] INT            IDENTITY(1,1) NOT NULL,
        [Folio]       UNIQUEIDENTIFIER NOT NULL,
        [Version]     INT            NOT NULL,
        [Nombre]      NVARCHAR(255)  NOT NULL,
        [FechaReg]    DATETIME2      NOT NULL CONSTRAINT [DF_DocumentosTesis_FechaReg] DEFAULT (SYSUTCDATETIME()),
        [FechaMod]    DATETIME2      NOT NULL CONSTRAINT [DF_DocumentosTesis_FechaMod] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_DocumentosTesis] PRIMARY KEY ([IdDocumento])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ConfirmacionesAsesoria')
BEGIN
    CREATE TABLE [dbo].[ConfirmacionesAsesoria] (
        [IdConfirmacion] INT                IDENTITY(1,1) NOT NULL,
        [Folio]          UNIQUEIDENTIFIER   NOT NULL,
        [IdProfesor]     INT                NOT NULL,
        [Tipo]           INT                NOT NULL,   -- 0 = Asesor, 1 = CoAsesor
        [Estado]         INT                NOT NULL,   -- 0 = Pendiente, 1 = Confirmada, 2 = Rechazada
        [Fecha]          DATETIME2          NOT NULL,
        [FechaReg]       DATETIME2          NOT NULL CONSTRAINT [DF_ConfirmacionesAsesoria_FechaReg] DEFAULT (SYSUTCDATETIME()),
        [FechaMod]       DATETIME2          NOT NULL CONSTRAINT [DF_ConfirmacionesAsesoria_FechaMod] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ConfirmacionesAsesoria] PRIMARY KEY ([IdConfirmacion])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'RevisionesIA')
BEGIN
    CREATE TABLE [dbo].[RevisionesIA] (
        [IdRevision] INT           IDENTITY(1,1) NOT NULL,
        [Folio]      UNIQUEIDENTIFIER NOT NULL,
        [Version]    INT           NOT NULL,
        [Resultado]  INT           NOT NULL,   -- 0 = Válido, 1 = No válido, 2 = Con observaciones
        [Porcentaje] DECIMAL(5,2)  NOT NULL,
        [Fecha]      DATETIME2     NOT NULL,
        [FechaReg]   DATETIME2     NOT NULL CONSTRAINT [DF_RevisionesIA_FechaReg] DEFAULT (SYSUTCDATETIME()),
        [FechaMod]   DATETIME2     NOT NULL CONSTRAINT [DF_RevisionesIA_FechaMod] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_RevisionesIA] PRIMARY KEY ([IdRevision])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'RevisionesAsesor')
BEGIN
    CREATE TABLE [dbo].[RevisionesAsesor] (
        [IdRevisionAsesor] INT                IDENTITY(1,1) NOT NULL,
        [Folio]            UNIQUEIDENTIFIER   NOT NULL,
        [IdProfesor]       INT                NOT NULL,
        [Version]          INT                NOT NULL,
        [EsUltimaVersion]  BIT                NOT NULL,
        [Decision]         INT                NOT NULL,   -- 0 = Aceptada, 1 = Rechazada
        [Observaciones]    NVARCHAR(1000)     NULL,
        [Fecha]            DATETIME2          NOT NULL,
        [FechaReg]         DATETIME2          NOT NULL CONSTRAINT [DF_RevisionesAsesor_FechaReg] DEFAULT (SYSUTCDATETIME()),
        [FechaMod]         DATETIME2          NOT NULL CONSTRAINT [DF_RevisionesAsesor_FechaMod] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_RevisionesAsesor] PRIMARY KEY ([IdRevisionAsesor])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Dictamenes')
BEGIN
    CREATE TABLE [dbo].[Dictamenes] (
        [IdDictamen]   INT              IDENTITY(1,1) NOT NULL,
        [Folio]        UNIQUEIDENTIFIER NOT NULL,
        [IdDirector]   INT              NOT NULL,       -- Id del usuario director (rol Director de posgrado)
        [Resultado]    INT              NOT NULL,       -- 0 = Aceptada, 1 = Rechazada
        [Observaciones] NVARCHAR(1000)  NULL,
        [Fecha]        DATETIME2        NOT NULL,
        [FechaReg]     DATETIME2        NOT NULL CONSTRAINT [DF_Dictamenes_FechaReg] DEFAULT (SYSUTCDATETIME()),
        [FechaMod]     DATETIME2        NOT NULL CONSTRAINT [DF_Dictamenes_FechaMod] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Dictamenes] PRIMARY KEY ([IdDictamen])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Articulos')
BEGIN
    CREATE TABLE [dbo].[Articulos] (
        [IdArticulo]      INT               IDENTITY(1,1) NOT NULL,
        [Folio]           UNIQUEIDENTIFIER  NOT NULL,
        [Nombre]          NVARCHAR(300)     NOT NULL,
        [Url]             NVARCHAR(500)     NOT NULL,
        [Doi]             NVARCHAR(200)     NULL,
        [IdTipoArticulo]  INT               NULL,
        [FechaReg]        DATETIME2         NOT NULL CONSTRAINT [DF_Articulos_FechaReg] DEFAULT (SYSUTCDATETIME()),
        [FechaMod]        DATETIME2         NOT NULL CONSTRAINT [DF_Articulos_FechaMod] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Articulos] PRIMARY KEY ([IdArticulo])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Favoritos')
BEGIN
    CREATE TABLE [dbo].[Favoritos] (
        [IdFavorito] INT               IDENTITY(1,1) NOT NULL,
        [IdUsuario]  INT               NOT NULL,
        [Folio]      UNIQUEIDENTIFIER  NOT NULL,
        [FechaReg]   DATETIME2         NOT NULL CONSTRAINT [DF_Favoritos_FechaReg] DEFAULT (SYSUTCDATETIME()),
        [FechaMod]   DATETIME2         NOT NULL CONSTRAINT [DF_Favoritos_FechaMod] DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Favoritos] PRIMARY KEY ([IdFavorito])
    );
END;
GO

/* ----------------------------------------------------------------------------
   CLAVES FORÁNEAS
   ---------------------------------------------------------------------------- */

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetRoleClaims_AspNetRoles_RoleId')
    ALTER TABLE [dbo].[AspNetRoleClaims]  ADD CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId]
        FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetUserClaims_AspNetUsers_UserId')
    ALTER TABLE [dbo].[AspNetUserClaims]   ADD CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetUserLogins_AspNetUsers_UserId')
    ALTER TABLE [dbo].[AspNetUserLogins]   ADD CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetUserRoles_AspNetUsers_UserId')
    ALTER TABLE [dbo].[AspNetUserRoles]    ADD CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetUserRoles_AspNetRoles_RoleId')
    ALTER TABLE [dbo].[AspNetUserRoles]    ADD CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId]
        FOREIGN KEY ([RoleId]) REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetUserRoles_AspNetUsers_UsuarioId')
    ALTER TABLE [dbo].[AspNetUserRoles]    ADD CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UsuarioId]
        FOREIGN KEY ([UsuarioId]) REFERENCES [dbo].[AspNetUsers] ([Id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetUserRoles_AspNetRoles_RolId')
    ALTER TABLE [dbo].[AspNetUserRoles]    ADD CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RolId]
        FOREIGN KEY ([RolId]) REFERENCES [dbo].[AspNetRoles] ([Id]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AspNetUserTokens_AspNetUsers_UserId')
    ALTER TABLE [dbo].[AspNetUserTokens]   ADD CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Alumnos_AspNetUsers_IdUsuario')
    ALTER TABLE [dbo].[Alumnos]            ADD CONSTRAINT [FK_Alumnos_AspNetUsers_IdUsuario]
        FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE RESTRICT;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Profesores_AspNetUsers_IdUsuario')
    ALTER TABLE [dbo].[Profesores]         ADD CONSTRAINT [FK_Profesores_AspNetUsers_IdUsuario]
        FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE RESTRICT;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProfesorPosgrado_Profesores_IdProfesor')
    ALTER TABLE [dbo].[ProfesorPosgrado]   ADD CONSTRAINT [FK_ProfesorPosgrado_Profesores_IdProfesor]
        FOREIGN KEY ([IdProfesor]) REFERENCES [dbo].[Profesores] ([IdProfesor]) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Tesis_Alumnos_IdAlumno')
    ALTER TABLE [dbo].[Tesis]              ADD CONSTRAINT [FK_Tesis_Alumnos_IdAlumno]
        FOREIGN KEY ([IdAlumno]) REFERENCES [dbo].[Alumnos] ([IdAlumno]) ON DELETE RESTRICT;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Tesis_Profesores_IdAsesor')
    ALTER TABLE [dbo].[Tesis]              ADD CONSTRAINT [FK_Tesis_Profesores_IdAsesor]
        FOREIGN KEY ([IdAsesor]) REFERENCES [dbo].[Profesores] ([IdProfesor]) ON DELETE RESTRICT;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Tesis_Profesores_IdCoAsesor')
    ALTER TABLE [dbo].[Tesis]              ADD CONSTRAINT [FK_Tesis_Profesores_IdCoAsesor]
        FOREIGN KEY ([IdCoAsesor]) REFERENCES [dbo].[Profesores] ([IdProfesor]) ON DELETE RESTRICT;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Articulos_Tesis_Folio')
    ALTER TABLE [dbo].[Articulos]          ADD CONSTRAINT [FK_Articulos_Tesis_Folio]
        FOREIGN KEY ([Folio]) REFERENCES [dbo].[Tesis] ([Folio]) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Articulos_TiposArticulo_IdTipoArticulo')
    ALTER TABLE [dbo].[Articulos]          ADD CONSTRAINT [FK_Articulos_TiposArticulo_IdTipoArticulo]
        FOREIGN KEY ([IdTipoArticulo]) REFERENCES [dbo].[TiposArticulo] ([IdTipoArticulo]) ON DELETE RESTRICT;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ConfirmacionesAsesoria_Tesis_Folio')
    ALTER TABLE [dbo].[ConfirmacionesAsesoria] ADD CONSTRAINT [FK_ConfirmacionesAsesoria_Tesis_Folio]
        FOREIGN KEY ([Folio]) REFERENCES [dbo].[Tesis] ([Folio]) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ConfirmacionesAsesoria_Profesores_IdProfesor')
    ALTER TABLE [dbo].[ConfirmacionesAsesoria] ADD CONSTRAINT [FK_ConfirmacionesAsesoria_Profesores_IdProfesor]
        FOREIGN KEY ([IdProfesor]) REFERENCES [dbo].[Profesores] ([IdProfesor]) ON DELETE RESTRICT;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Dictamenes_Tesis_Folio')
    ALTER TABLE [dbo].[Dictamenes]         ADD CONSTRAINT [FK_Dictamenes_Tesis_Folio]
        FOREIGN KEY ([Folio]) REFERENCES [dbo].[Tesis] ([Folio]) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DocumentosTesis_Tesis_Folio')
    ALTER TABLE [dbo].[DocumentosTesis]    ADD CONSTRAINT [FK_DocumentosTesis_Tesis_Folio]
        FOREIGN KEY ([Folio]) REFERENCES [dbo].[Tesis] ([Folio]) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Favoritos_AspNetUsers_IdUsuario')
    ALTER TABLE [dbo].[Favoritos]          ADD CONSTRAINT [FK_Favoritos_AspNetUsers_IdUsuario]
        FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE RESTRICT;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Favoritos_Tesis_Folio')
    ALTER TABLE [dbo].[Favoritos]          ADD CONSTRAINT [FK_Favoritos_Tesis_Folio]
        FOREIGN KEY ([Folio]) REFERENCES [dbo].[Tesis] ([Folio]) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RevisionesAsesor_Tesis_Folio')
    ALTER TABLE [dbo].[RevisionesAsesor]   ADD CONSTRAINT [FK_RevisionesAsesor_Tesis_Folio]
        FOREIGN KEY ([Folio]) REFERENCES [dbo].[Tesis] ([Folio]) ON DELETE CASCADE;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RevisionesAsesor_Profesores_IdProfesor')
    ALTER TABLE [dbo].[RevisionesAsesor]   ADD CONSTRAINT [FK_RevisionesAsesor_Profesores_IdProfesor]
        FOREIGN KEY ([IdProfesor]) REFERENCES [dbo].[Profesores] ([IdProfesor]) ON DELETE RESTRICT;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RevisionesIA_Tesis_Folio')
    ALTER TABLE [dbo].[RevisionesIA]       ADD CONSTRAINT [FK_RevisionesIA_Tesis_Folio]
        FOREIGN KEY ([Folio]) REFERENCES [dbo].[Tesis] ([Folio]) ON DELETE CASCADE;
GO

/* ----------------------------------------------------------------------------
   ÍNDICES ÚNICOS (integridad de negocio)
   ---------------------------------------------------------------------------- */

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_AspNetRoles_NormalizedName')
    CREATE UNIQUE INDEX [UQ_AspNetRoles_NormalizedName] ON [dbo].[AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_AspNetUsers_NormalizedUserName')
    CREATE UNIQUE INDEX [UQ_AspNetUsers_NormalizedUserName] ON [dbo].[AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_AspNetUsers_NormalizedEmail')
    CREATE UNIQUE INDEX [UQ_AspNetUsers_NormalizedEmail] ON [dbo].[AspNetUsers] ([NormalizedEmail]) WHERE [NormalizedEmail] IS NOT NULL;
GO

-- Un alumno solo puede existir una vez por número de control
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Alumnos_NumeroControl')
    CREATE UNIQUE INDEX [UQ_Alumnos_NumeroControl] ON [dbo].[Alumnos] ([NumeroControl]);
GO

-- Un usuario no puede tener la misma tesis en favoritos dos veces
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Favoritos_IdUsuario_Folio')
    CREATE UNIQUE INDEX [UQ_Favoritos_IdUsuario_Folio] ON [dbo].[Favoritos] ([IdUsuario], [Folio]);
GO

-- Una tesis es asesorada una sola vez por profesor y tipo
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_ConfirmacionesAsesoria_Folio_Profesor_Tipo')
    CREATE UNIQUE INDEX [UQ_ConfirmacionesAsesoria_Folio_Profesor_Tipo] ON [dbo].[ConfirmacionesAsesoria] ([Folio], [IdProfesor], [Tipo]);
GO

-- Índices de búsqueda frecuente
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tesis_Estado')
    CREATE NONCLUSTERED INDEX [IX_Tesis_Estado] ON [dbo].[Tesis] ([Estado]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Articulos_Folio')
    CREATE NONCLUSTERED INDEX [IX_Articulos_Folio] ON [dbo].[Articulos] ([Folio]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Favoritos_Folio')
    CREATE NONCLUSTERED INDEX [IX_Favoritos_Folio] ON [dbo].[Favoritos] ([Folio]);
GO

/* ----------------------------------------------------------------------------
   RESTRICCIONES CHECK (valores de catálogo embebidos en el modelo C#)
   ---------------------------------------------------------------------------- */

-- Tesis.Estado: 0 Registrada | 1 Confirmada | 2 EnRevisionIA | 3 EnRevisionDelAsesor
--               4 CambiosSolicitados | 5 EnDictamen | 6 Aceptada | 7 Rechazada | 8 Inactiva
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Tesis_Estado')
    ALTER TABLE [dbo].[Tesis] ADD CONSTRAINT [CK_Tesis_Estado]
        CHECK ([Estado] BETWEEN 0 AND 8);
GO

-- Tesis.Nivel: 0 Maestría | 1 Doctorado
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Tesis_Nivel')
    ALTER TABLE [dbo].[Tesis] ADD CONSTRAINT [CK_Tesis_Nivel]
        CHECK ([Nivel] IN (0, 1));
GO

-- ConfirmacionAsesoria.Tipo: 0 Asesor | 1 CoAsesor
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Confirmacion_Tipo')
    ALTER TABLE [dbo].[ConfirmacionesAsesoria] ADD CONSTRAINT [CK_Confirmacion_Tipo]
        CHECK ([Tipo] IN (0, 1));
GO

-- ConfirmacionAsesoria.Estado: 0 Pendiente | 1 Confirmada | 2 Rechazada
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Confirmacion_Estado')
    ALTER TABLE [dbo].[ConfirmacionesAsesoria] ADD CONSTRAINT [CK_Confirmacion_Estado]
        CHECK ([Estado] IN (0, 1, 2));
GO

-- RevisionIA.Resultado: 0 Válido | 1 No válido | 2 Con observaciones
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RevisionIA_Resultado')
    ALTER TABLE [dbo].[RevisionesIA] ADD CONSTRAINT [CK_RevisionIA_Resultado]
        CHECK ([Resultado] IN (0, 1, 2));
GO

-- RevisionAsesor.Decision: 0 Aceptada | 1 Rechazada
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RevisionAsesor_Decision')
    ALTER TABLE [dbo].[RevisionesAsesor] ADD CONSTRAINT [CK_RevisionAsesor_Decision]
        CHECK ([Decision] IN (0, 1));
GO

-- Dictamen.Resultado: 0 Aceptada | 1 Rechazada
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Dictamen_Resultado')
    ALTER TABLE [dbo].[Dictamenes] ADD CONSTRAINT [CK_Dictamen_Resultado]
        CHECK ([Resultado] IN (0, 1));
GO

-- RevisionIA.Porcentaje: 0.00 a 100.00
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RevisionIA_Porcentaje')
    ALTER TABLE [dbo].[RevisionesIA] ADD CONSTRAINT [CK_RevisionIA_Porcentaje]
        CHECK ([Porcentaje] >= 0 AND [Porcentaje] <= 100);
GO

/* ----------------------------------------------------------------------------
   DATOS SEMILLA — Catálogos (Roles, TipoArticulo)
   Nota: AspNetUsers no se siembra aquí; lo hace Identity en el arranque de la
   API junto con cuentas demo (contraseñas con hash). Los valores de "Estados"
   se documentan en los CHECK superiores y en el modelo de dominio.
   ---------------------------------------------------------------------------- */

-- Roles del sistema (6)
INSERT INTO [dbo].[AspNetRoles] ([Name], [NormalizedName], [ConcurrencyStamp])
SELECT v.[Name], v.[NormalizedName], NEWID()
FROM (VALUES
    (N'Invitado',              N'INVITADO'),
    (N'Alumno',                N'ALUMNO'),
    (N'Docente',               N'DOCENTE'),
    (N'Director de posgrado',  N'DIRECTOR DE POSGRADO'),
    (N'Administrador',         N'ADMINISTRADOR'),
    (N'Administrador de TI',   N'ADMINISTRADOR DE TI')
) AS v([Name], [NormalizedName])
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[AspNetRoles] r
    WHERE r.[NormalizedName] = v.[NormalizedName]
);
GO

-- Catálogo TipoArticulo (coincide con la siembra del arranque de la API)
INSERT INTO [dbo].[TiposArticulo] ([Nombre], [Descripcion], [Activo])
SELECT v.[Nombre], v.[Descripcion], 1
FROM (VALUES
    (N'Artículo de revista',  N'Publicación académica arbitrada.'),
    (N'Artículo de congreso', N'Memorias y proceedings de congresos.'),
    (N'Capítulo de libro',    N'Contribución en volumen colectivo.'),
    (N'Libro',                N'Obra completa publicada.')
) AS v([Nombre], [Descripcion])
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[TiposArticulo] t
    WHERE t.[Nombre] = v.[Nombre]
);
GO

-- Catálogo Posgrados (Maestrías y Doctorados vigentes de la UPT)
INSERT INTO [dbo].[Posgrados] ([Nivel], [Nombre], [Activo], [FechaReg], [FechaMod])
SELECT v.[Nivel], v.[Nombre], 1, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM (VALUES
    (0, N'Maestría en Energías Renovables'),
    (0, N'Maestría en Computación Óptica'),
    (0, N'Maestría en Dirección de Organizaciones'),
    (0, N'Maestría en Desarrollo de Software'),
    (0, N'Maestría en Automatización y Control'),
    (0, N'Maestría en Optimización de Procesos'),
    (0, N'Maestría en Contribuciones Fiscales'),
    (0, N'Maestría en Gestión e Innovación Educativa'),
    (1, N'Doctorado en Optomecatrónica'),
    (1, N'Doctorado en Ciencias de la Gestión Administrativa')
) AS v([Nivel], [Nombre])
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Posgrados] p
    WHERE p.[Nombre] = v.[Nombre] AND p.[Nivel] = v.[Nivel]
);
GO