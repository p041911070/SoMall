import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { environment } from '@env/environment';
// dashboard pages
import { DashboardAnalysisComponent } from './dashboard/analysis/analysis.component';
import { DashboardWorkplaceComponent } from './dashboard/workplace/workplace.component';
// single pages

import { UserLockComponent } from './passport/lock/lock.component';
import { Demo1Component } from './demo/demo1.component';
import { AuthGuard } from '@core/auth-guard.service';
import { Exception404Component } from './exception/404.component';
import { AuthCallbackComponent } from './auth-callback/auth-callback.component';
import { LayoutComponent } from '../layout/layout.component';
import { MenuService } from '@core/menu/menu.service';
import { TranslatorService } from '@core/translator/translator.service';
import { menu } from './menu';

const routes: Routes = [
  {
    path: '',
    component: LayoutComponent,
    // canActivate: [AuthGuard],
    children: [
      { path: '', redirectTo: 'dashboard/workplace', pathMatch: 'full' },
      { path: 'dashboard', redirectTo: 'dashboard/workplace', pathMatch: 'full' },
      { path: 'dashboard/analysis', component: DashboardAnalysisComponent },
      { path: 'dashboard/workplace', component: DashboardWorkplaceComponent }
    ],
  },
  { path: 'identity', loadChildren: () => import(/* webpackChunkName: "IdentityModule" */'../identity/identity.module').then(m => m.IdentityModule) },
  { path: 'tenant', loadChildren: () => import(/* webpackChunkName: "TenantModule" */'../tenant/tenant.module').then(m => m.TenantModule) },
  { path: 'app-management', loadChildren: () => import(/* webpackChunkName: "AppManagementModule" */'../app-management/app-management.module').then(m => m.AppManagementModule) },

  { path: 'shop-management', loadChildren: () => import(/* webpackChunkName: "ShopManagementModule" */ '../shop-management/shop-management.module').then(m => m.ShopManagementModule), data: { breadcrumb: "商家管理" } },
  { path: 'mall', loadChildren: () => import(/* webpackChunkName: "MallModule" */ '../mall/mall.module').then(m => m.MallModule), data: { breadcrumb: "商城系统" } },

  { path: 'visitor', loadChildren: () => import(/* webpackChunkName: "VisitorModule" */ '../visitor/visitor.module').then(m => m.VisitorModule), data: { breadcrumb: "访客管理系统" } },

  { path: 'account-management', loadChildren: () => import(/* webpackChunkName: "AccountManagementModule" */ '../account-management/account-management.module').then(m => m.AccountManagementModule), data: { breadcrumb: "微信用户管理" } },

  { path: 'auditLog', loadChildren: () => import(/* webpackChunkName: "AuditLogModule" */ '../auditLog/auditLog.module').then(m => m.AuditLogModule), data: { breadcrumb: "审计日志" } },
  
  { path: 'cms', loadChildren: () => import(/* webpackChunkName: "CmsModule" */ '../cms/cms.module').then(m => m.CmsModule), data: { breadcrumb: "CMS" } },
  {
    path: 'exception',
    component: LayoutComponent,
    children: [
      {
        path: '404', component: Exception404Component, data: { title: '404' },
      }
    ],
  },
  // 单页不包裹Layout
  { path: 'auth-callback', component: AuthCallbackComponent },
  { path: 'lock', component: UserLockComponent, data: { title: '锁屏', titleI18n: 'app.lock' } },
  { path: '**', redirectTo: 'exception/404' },
];

@NgModule({
  imports: [
    RouterModule.forRoot(routes, {
      useHash: environment.useHash,
      scrollPositionRestoration: 'top',
    }),
  ],
  exports: [RouterModule],
})
export class RouteRoutingModule {
  constructor(public menuService: MenuService, tr: TranslatorService) {
    menuService.addMenu(menu);
  }
}
