/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

/**
 * 个人信息更改
 */
export type ProfileUpdateModel = {
    /**
     * 用户名
     */
    userName?: string | null;
    /**
     * 描述
     */
    bio?: string | null;
    /**
     * 手机号
     */
    phone?: string | null;
    /**
     * 真实姓名
     */
    realName?: string | null;
};
