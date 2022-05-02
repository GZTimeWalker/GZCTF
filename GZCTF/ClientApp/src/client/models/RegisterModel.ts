/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

/**
 * 注册账号
 */
export type RegisterModel = {
    /**
     * 用户名
     */
    userName: string;
    /**
     * 密码
     */
    password: string;
    /**
     * 邮箱
     */
    email: string;
    /**
     * Google Recaptcha Token
     */
    gToken?: string | null;
};
