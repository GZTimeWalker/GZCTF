/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { Role } from './Role';

export type ClientUserInfoModel = {
    /**
     * 用户ID
     */
    userId?: string | null;
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
     * 头像链接
     */
    avatar?: string | null;
    /**
     * 当前队伍
     */
    activeTeamId?: number | null;
    /**
     * 用户角色
     */
    role?: Role | null;
};
