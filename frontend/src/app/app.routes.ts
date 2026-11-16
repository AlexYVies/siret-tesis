import { Routes } from '@angular/router';

import { autenticado, conRol, estaAutenticadoMatch, iniciadoSesion } from './core/guards/acceso.guard';
import { Rol } from './core/models/constantes.model';
import { PanelLayout } from './layout/panel/panel.layout';
import { PublicoLayout } from './layout/publico/publico.layout';
import { InicioPage } from './pages/inicio/inicio.page';
import { LoginPage } from './pages/login/login.page';
import { RegistroPage } from './pages/registro/registro.page';
import { RecuperarContrasenaPage } from './pages/recuperar-contrasena/recuperar-contrasena.page';
import { RestablecerContrasenaPage } from './pages/restablecer-contrasena/restablecer-contrasena.page';
import { ValidarCuentaPage } from './pages/validar-cuenta/validar-cuenta.page';
import { ListaTesisPage } from './pages/tesis/lista-tesis.page';
import { RegistrarTesisPage } from './pages/tesis/registrar-tesis.page';
import { DetalleTesisPage } from './pages/tesis/detalle-tesis.page';
import { PerfilPage } from './pages/perfil/perfil.page';
import { CuentaPage } from './pages/cuenta/cuenta.page';
import { RepositorioPage } from './pages/repositorio/repositorio.page';
import { FavoritosPage } from './pages/favoritos/favoritos.page';
import { IndicadoresPage } from './pages/indicadores/indicadores.page';
import { AdministracionPage } from './pages/administracion/administracion.page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'inicio' },
  {
    path: 'login',
    component: LoginPage,
    canActivate: [iniciadoSesion],
  },
  {
    path: 'registro',
    component: RegistroPage,
    canActivate: [iniciadoSesion],
  },
  {
    path: 'validar-cuenta',
    component: ValidarCuentaPage,
    canActivate: [iniciadoSesion],
  },
  {
    path: 'recuperar-contrasena',
    component: RecuperarContrasenaPage,
    canActivate: [iniciadoSesion],
  },
  {
    path: 'restablecer-contrasena',
    component: RestablecerContrasenaPage,
    canActivate: [iniciadoSesion],
  },
  {
    path: '',
    component: PanelLayout,
    canMatch: [estaAutenticadoMatch],
    canActivate: [autenticado],
    children: [
      {
        path: 'tesis',
        component: ListaTesisPage,
        canActivate: [conRol(Rol.Director, Rol.AdministradorTI, Rol.Docente, Rol.Alumno, Rol.Invitado)],
      },
      { path: 'mis-tesis', component: ListaTesisPage },
      { path: 'asesorias', component: ListaTesisPage },
      { path: 'repositorio', component: RepositorioPage },
      { path: 'tesis/registrar', component: RegistrarTesisPage },
      { path: 'tesis/:folio', component: DetalleTesisPage },
      { path: 'cuenta', component: CuentaPage },
      { path: 'perfil', redirectTo: 'cuenta', pathMatch: 'full' },
      { path: 'favoritos', component: FavoritosPage },
      {
        path: 'administracion',
        component: AdministracionPage,
        canActivate: [conRol(Rol.Administrador, Rol.AdministradorTI)],
      },
      { path: 'indicadores', component: IndicadoresPage, canActivate: [conRol(Rol.Director)] },
    ],
  },
  {
    path: '',
    component: PublicoLayout,
    children: [
      { path: 'inicio', component: InicioPage },
      { path: 'repositorio', component: RepositorioPage },
    ],
  },
  { path: '**', redirectTo: 'inicio' },
];