/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

/**
 * 账号验证
 */
export type AccountVerifyModel = {
    /**
     * 邮箱接收到的Base64格式Token
     */
    token: string;
    /**
     * 用户邮箱的Base64格式
     */
    email: string;
};
