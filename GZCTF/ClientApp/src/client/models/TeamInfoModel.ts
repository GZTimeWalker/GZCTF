/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { BasicTeamUserInfoModel } from './BasicTeamUserInfoModel';

export type TeamInfoModel = {
    /**
     * 队伍 Id
     */
    id?: number;
    /**
     * 队伍名称
     */
    name?: string | null;
    /**
     * 队伍签名
     */
    bio?: string | null;
    /**
     * 头像链接
     */
    avatar?: string | null;
    /**
     * 是否锁定
     */
    locked?: boolean;
    /**
     * 队伍成员
     */
    members?: Array<BasicTeamUserInfoModel> | null;
};
