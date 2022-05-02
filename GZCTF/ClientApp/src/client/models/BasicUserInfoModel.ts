/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { Role } from './Role';

export type BasicUserInfoModel = {
    /**
     * 用户ID
     */
    id?: string | null;
    /**
     * 用户名
     */
    userName?: string | null;
    /**
     * 邮箱
     */
    email?: string | null;
    /**
     * 头像链接
     */
    avatar?: string | null;
    /**
     * 用户角色
     */
    role?: Role | null;
    /**
     * 所拥有的队伍
     */
    ownTeamName?: string | null;
    ownTeamId?: number | null;
    /**
     * 激活的队伍
     */
    activeTeamName?: string | null;
    activeTeamId?: number | null;
};
