import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { AuthGuard } from '@core/auth-guard.service';
import { LayoutComponent } from '../layout/layout.component';
import { WechatUserinfoListComponent } from './components/wechatUserinfo-list/wechatUserinfo-list.component';
import { RealNameInfoListComponent } from './components/realNameInfo-list/realNameInfo-list.component';


const routes: Routes = [
  { path: '', redirectTo: 'wechatUserInfos', pathMatch: 'full' },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [AuthGuard],
    children: [
      { path: 'wechatUserInfos', component: WechatUserinfoListComponent, data: { title: '微信用户列表', permission: 'Pages' } },
      { path: 'realNameInfos', component: RealNameInfoListComponent, data: { title: '实名认证列表', permission: 'Pages' } },
    ],
  },
]


@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AccountManagementRoutingModule { }
