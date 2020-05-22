import { VuexModule, Module, Mutation, Action, getModule } from 'vuex-module-decorators'
import store from '@/store'
import api from '@/utils/api';

export interface IUser {
    id?: boolean;
    isAuthenticated?: boolean;
    userName?: string;
    name?: string;
    surname?: string;
    nickname?: string;
    headImgUrl?: string;
    phoneNumber?: string;
    phoneNumberConfirmed?: boolean;
    email?: string;
    tenantId?: string;
    roles?: string[];
}


export interface IUserInfo {
    avatarUrl?: string;
    city?: string;
    country?: string;
    gender?: number;
    language?: string;
    nickName?: string;
    openid?: string;
    province?: string;
    unionid?: string;
}

export interface IShopMember {
    Id?: number;
    Balance?: number;
    DateTimeCreate?: Date;
    Name?: string;
    OrderCount?: number;
    StoreId?: number;
    Telphone?: string;
    nickname?: string;
    openid?: string;
    unionid?: string;
}

@Module({ dynamic: true, store, name: 'user' })
class User extends VuexModule {

    private user: IUser = {}

    private userInfo: IUserInfo = uni.getStorageSync("userInfo") || {
        openid: "",
        unionid: ""
    };
    private token: string = uni.getStorageSync("token") || "";
    private sessionKey: string = uni.getStorageSync("sessionKey") || "";
    private phone: string = uni.getStorageSync("phone") || "";

    get getUser() {
        return this.user;
    }

    get getUserInfo() {
        return this.userInfo;
    }

    get getPhone() {
        return this.phone;
    }

    get getToken() {
        return this.token;
    }

    get getOpenid() {
        return this.userInfo.openid;
    }

    get getSessionKey() {
        return this.sessionKey;
    }


    @Mutation
    private SET_USER(payload: IUser) { this.user = payload }

    @Mutation
    private SET_USERINFO(v: IUserInfo) {
        uni.setStorageSync("userInfo", v);
        if (v.openid)
            uni.setStorageSync("openid", v.openid);
        if (v.unionid)
            uni.setStorageSync("unionid", v.unionid);
        this.userInfo = v;
    }

    @Mutation
    private SET_SESSIONKEY(v: string) {
        uni.setStorageSync("sessionKey", v);
        this.sessionKey = v;
    }
    @Mutation
    private SET_TOKEN(v: string) {
        uni.setStorageSync("token", v);
        this.token = v;
    }

    @Mutation
    private SET_PHONE(payload: string) {
        uni.setStorageSync("phone", payload);
        this.phone = payload;
    }

    @Mutation
    private LOGOUT() {
        console.log("mutaction:LOGOUT")
        uni.removeStorageSync("token");
        uni.removeStorageSync("openid");
        uni.removeStorageSync("unionid");
        uni.removeStorageSync("userInfo");
        uni.removeStorageSync("sessionKey");
        this.token = "";
        this.userInfo = {
            openid: "",
            unionid: ""
        };
        this.sessionKey = ""
    }

    @Action
    public Login(v: any = null) {
        return new Promise((resolve, reject) => {
            uni.login({
                success: logRes => {
                    uni.getUserInfo({
                        success: (res1: any) => {
                            res1.code = logRes.code;
                            console.log("uni.getUserInfo:", res1)
                            this.SET_USERINFO(res1.userInfo);
                            if (res1) {
                                api
                                    .client_miniAuth({
                                        code: res1.code,
                                        iv: res1.iv,
                                        encryptedData: res1.encryptedData
                                    })
                                    .then((res: any) => {
                                        if (res.accessToken) {
                                            this.SET_TOKEN(res.accessToken)
                                            this.SET_USERINFO(res.externalUser);
                                            this.SET_SESSIONKEY(res.sessionKey);
                                            this.CheckLogin();
                                            return resolve(res);
                                        } else {
                                            return reject("获取登录失败");
                                        }
                                    });
                            }
                        }
                    });
                },
                fail: error => {
                    return reject(error);
                }
            });
        });
    }

    // Logout
    @Action
    public Logout() {
        this.LOGOUT();
    }

    @Action
    public async CheckLogin() {
        await api.checkLogin().then((res: any) => {
            if (res && res.isAuthenticated) {
                this.SET_USER(res);
                if (res.phoneNumber && res.phoneNumberConfirmed)
                    this.SET_PHONE(res.phoneNumber);
            }
            else {
                console.log("notlogin... to logout")
                console.log(res)
                this.LOGOUT();
            }
        }).catch(() => {
            console.log("notlogin")
            this.LOGOUT();
        })
    }

    @Action
    SetPhone(phone: string) {
        this.SET_PHONE(phone);
    }
}

export const UserModule = getModule(User)
