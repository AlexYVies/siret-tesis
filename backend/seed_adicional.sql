USE TesisUPT;
GO

-- 1. Favoritos de prueba para alumno.demo (IdUsuario = 1)
IF NOT EXISTS (SELECT 1 FROM Favoritos WHERE IdUsuario = 1)
BEGIN
    INSERT INTO Favoritos (IdUsuario, Folio, FechaReg, FechaMod)
    SELECT 1, Folio, GETUTCDATE(), GETUTCDATE()
    FROM Tesis
    WHERE Estado = 6;
END;
GO

-- 2. Tesis 1 para alumno.demo: 'EnRevisionDelAsesor' (Estado = 3)
DECLARE @folioDemo1 UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
IF NOT EXISTS (SELECT 1 FROM Tesis WHERE Folio = @folioDemo1)
BEGIN
    INSERT INTO Tesis (Folio, IdAlumno, Titulo, Nivel, Programa, IdAsesor, TieneCoAsesor, IdCoAsesor, Version, Estado, FechaExamenGrado, FechaReg, FechaMod)
    VALUES (
        @folioDemo1,
        1,
        'Plataforma IoT con edge computing para agricultura de precision en Hidalgo',
        0, -- Maestria
        'Maestria en Tecnologias de la Informacion',
        1, -- Dra. Ana Beltran
        1, -- Tiene coasesor
        2, -- Dr. Luis Cortes
        1,
        3, -- EnRevisionDelAsesor
        NULL,
        GETUTCDATE(),
        GETUTCDATE()
    );

    INSERT INTO ConfirmacionesAsesoria (Folio, IdProfesor, Tipo, Estado, Fecha, FechaReg, FechaMod)
    VALUES
        (@folioDemo1, 1, 0, 1, GETUTCDATE(), GETUTCDATE(), GETUTCDATE()),
        (@folioDemo1, 2, 1, 1, GETUTCDATE(), GETUTCDATE(), GETUTCDATE());

    INSERT INTO DocumentosTesis (Folio, Version, Nombre, FechaReg, FechaMod)
    VALUES (@folioDemo1, 1, 'tesis-iot-precision.pdf', GETUTCDATE(), GETUTCDATE());

    INSERT INTO RevisionesIA (Folio, Version, Resultado, Porcentaje, Fecha, FechaReg, FechaMod)
    VALUES (@folioDemo1, 1, 0, 96.50, GETUTCDATE(), GETUTCDATE(), GETUTCDATE());
END;
GO

-- 3. Tesis 2 para alumno.demo: 'Registrada' con confirmacion pendiente (Estado = 0)
DECLARE @folioDemo2 UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
IF NOT EXISTS (SELECT 1 FROM Tesis WHERE Folio = @folioDemo2)
BEGIN
    INSERT INTO Tesis (Folio, IdAlumno, Titulo, Nivel, Programa, IdAsesor, TieneCoAsesor, IdCoAsesor, Version, Estado, FechaExamenGrado, FechaReg, FechaMod)
    VALUES (
        @folioDemo2,
        1,
        'Analisis predictivo de desercion escolar mediante grafos de conocimiento',
        0, -- Maestria
        'Maestria en Tecnologias de la Informacion',
        1, -- Dra. Ana Beltran
        0, -- Sin coasesor
        NULL,
        1,
        0, -- Registrada
        NULL,
        GETUTCDATE(),
        GETUTCDATE()
    );

    INSERT INTO ConfirmacionesAsesoria (Folio, IdProfesor, Tipo, Estado, Fecha, FechaReg, FechaMod)
    VALUES (@folioDemo2, 1, 0, 0, GETUTCDATE(), GETUTCDATE(), GETUTCDATE());
END;
GO

-- 4. Tesis 3 para alumno.demo: 'EnDictamen' (Estado = 5)
DECLARE @folioDemo3 UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';
IF NOT EXISTS (SELECT 1 FROM Tesis WHERE Folio = @folioDemo3)
BEGIN
    INSERT INTO Tesis (Folio, IdAlumno, Titulo, Nivel, Programa, IdAsesor, TieneCoAsesor, IdCoAsesor, Version, Estado, FechaExamenGrado, FechaReg, FechaMod)
    VALUES (
        @folioDemo3,
        1,
        'Criptografia poscuantica aplicada a canales de comunicacion satelital',
        0, -- Maestria
        'Maestria en Tecnologias de la Informacion',
        1, -- Dra. Ana Beltran
        0,
        NULL,
        1,
        5, -- EnDictamen
        NULL,
        GETUTCDATE(),
        GETUTCDATE()
    );

    INSERT INTO ConfirmacionesAsesoria (Folio, IdProfesor, Tipo, Estado, Fecha, FechaReg, FechaMod)
    VALUES (@folioDemo3, 1, 0, 1, GETUTCDATE(), GETUTCDATE(), GETUTCDATE());

    INSERT INTO DocumentosTesis (Folio, Version, Nombre, FechaReg, FechaMod)
    VALUES (@folioDemo3, 1, 'tesis-cripto-poscuantica.pdf', GETUTCDATE(), GETUTCDATE());

    INSERT INTO RevisionesIA (Folio, Version, Resultado, Porcentaje, Fecha, FechaReg, FechaMod)
    VALUES (@folioDemo3, 1, 0, 91.00, GETUTCDATE(), GETUTCDATE(), GETUTCDATE());

    INSERT INTO RevisionesAsesor (Folio, IdProfesor, Version, EsUltimaVersion, Decision, Observaciones, Fecha, FechaReg, FechaMod)
    VALUES (@folioDemo3, 1, 1, 1, 0, 'Revision aprobada sin observaciones.', GETUTCDATE(), GETUTCDATE(), GETUTCDATE());
END;
GO
