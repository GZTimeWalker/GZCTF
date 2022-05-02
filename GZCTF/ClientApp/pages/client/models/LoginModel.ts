/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

/**
 * 登录
 */
export type LoginModel = {
    /**
     * 用户名或邮箱
     */
    userName: string;
    /**
     * 密码
     */
    password: string;
    /**
     * Google Recaptcha Token
     */
    gToken?: string | null;
};
