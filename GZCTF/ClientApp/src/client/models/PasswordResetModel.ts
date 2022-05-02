/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

/**
 * 密码重置
 */
export type PasswordResetModel = {
    /**
     * 密码
     */
    password: string;
    /**
     * 邮箱
     */
    email: string;
    /**
     * 邮箱接收到的Base64格式Token
     */
    rToken: string;
};
