/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { Role } from './Role';

export type UpdateUserInfoModel = {
    /**
     * 用户名
     */
    userName?: string | null;
    /**
     * 邮箱
     */
    email?: string | null;
    /**
     * 签名
     */
    bio?: string | null;
    /**
     * 手机号码
     */
    phone?: string | null;
    /**
     * 真实姓名
     */
    realName?: string | null;
    /**
     * 用户角色
     */
    role?: Role | null;
};
