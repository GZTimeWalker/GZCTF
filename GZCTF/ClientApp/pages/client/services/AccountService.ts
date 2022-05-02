/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { AccountVerifyModel } from '../models/AccountVerifyModel';
import type { ClientUserInfoModel } from '../models/ClientUserInfoModel';
import type { LoginModel } from '../models/LoginModel';
import type { MailChangeModel } from '../models/MailChangeModel';
import type { PasswordChangeModel } from '../models/PasswordChangeModel';
import type { PasswordResetModel } from '../models/PasswordResetModel';
import type { ProfileUpdateModel } from '../models/ProfileUpdateModel';
import type { RecoveryModel } from '../models/RecoveryModel';
import type { RegisterModel } from '../models/RegisterModel';
import type { RequestResponse } from '../models/RequestResponse';

import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class AccountService {

    /**
     * 用户注册接口
     * 使用此接口注册新用户，Dev环境下不校验 GToken，邮件URL：/verify
     * @param requestBody
     * @returns any 注册成功
     * @throws ApiError
     */
    public static accountRegister(
        requestBody: RegisterModel,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/account/register',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `校验失败或用户已存在`,
            },
        });
    }

    /**
     * 用户找回密码请求接口
     * 使用此接口请求找回密码，向用户邮箱发送邮件，邮件URL：/reset
     * @param requestBody
     * @returns RequestResponse 用户密码重置邮件发送成功
     * @throws ApiError
     */
    public static accountRecovery(
        requestBody: RecoveryModel,
    ): CancelablePromise<RequestResponse> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/account/recovery',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `校验失败`,
                404: `用户不存在`,
            },
        });
    }

    /**
     * 用户重置密码接口
     * 使用此接口重置密码，需要邮箱验证码
     * @param requestBody
     * @returns any 用户成功重置密码
     * @throws ApiError
     */
    public static accountPasswordReset(
        requestBody: PasswordResetModel,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/account/passwordreset',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `校验失败`,
            },
        });
    }

    /**
     * 用户邮箱确认接口
     * 使用此接口通过邮箱验证码确认邮箱
     * @param requestBody
     * @returns any 用户通过邮箱验证
     * @throws ApiError
     */
    public static accountVerify(
        requestBody: AccountVerifyModel,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/account/verify',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `校验失败`,
                401: `邮箱验证失败`,
            },
        });
    }

    /**
     * 用户登录接口
     * 使用此接口登录账户，Dev环境下不校验 GToken
     * @param requestBody
     * @returns any 用户成功登录
     * @throws ApiError
     */
    public static accountLogIn(
        requestBody: LoginModel,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/account/login',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                401: `用户名或密码错误`,
            },
        });
    }

    /**
     * 用户登出接口
     * 使用此接口登出账户，需要User权限
     * @returns any 用户已登出
     * @throws ApiError
     */
    public static accountLogOut(): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/account/logout',
        });
    }

    /**
     * 用户数据更新接口
     * 使用此接口更新用户用户名和描述，需要User权限
     * @param requestBody
     * @returns any 用户数据成功更新
     * @throws ApiError
     */
    public static accountUpdate(
        requestBody: ProfileUpdateModel,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/account/update',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `校验失败或用户数据更新失败`,
            },
        });
    }

    /**
     * 用户密码更改接口
     * 使用此接口更新用户密码，需要User权限
     * @param requestBody
     * @returns any 用户成功更新密码
     * @throws ApiError
     */
    public static accountChangePassword(
        requestBody: PasswordChangeModel,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/account/changepassword',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                401: `无权访问`,
            },
        });
    }

    /**
     * 用户邮箱更改接口
     * 使用此接口更改用户邮箱，需要User权限，邮件URL：/confirm
     * @param requestBody
     * @returns any 成功发送用户邮箱更改邮件
     * @throws ApiError
     */
    public static accountChangeEmail(
        requestBody: MailChangeModel,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/account/changeemail',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `校验失败或邮箱已经被占用`,
                401: `无权访问`,
            },
        });
    }

    /**
     * 用户邮箱更改确认接口
     * 使用此接口确认更改用户邮箱，需要邮箱验证码，需要User权限
     * @param requestBody
     * @returns any 用户成功更改邮箱
     * @throws ApiError
     */
    public static accountMailChangeConfirm(
        requestBody: AccountVerifyModel,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/account/mailchangeconfirm',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `校验失败或无效邮箱`,
                401: `未授权用户`,
            },
        });
    }

    /**
     * 获取用户信息接口
     * 使用此接口获取用户信息，需要User权限
     * @returns ClientUserInfoModel 用户成功获取信息
     * @throws ApiError
     */
    public static accountProfile(): CancelablePromise<ClientUserInfoModel> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/account/profile',
            errors: {
                401: `未授权用户`,
            },
        });
    }

    /**
     * 更新用户头像接口
     * 使用此接口更新用户头像，需要User权限
     * @param formData
     * @returns string 用户头像URL
     * @throws ApiError
     */
    public static accountAvatar(
        formData?: {
            file?: Blob;
        },
    ): CancelablePromise<string> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/account/avatar',
            formData: formData,
            mediaType: 'multipart/form-data',
            errors: {
                400: `非法请求`,
                401: `未授权用户`,
            },
        });
    }

}