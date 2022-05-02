/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { BasicUserInfoModel } from '../models/BasicUserInfoModel';
import type { ClientUserInfoModel } from '../models/ClientUserInfoModel';
import type { LocalFile } from '../models/LocalFile';
import type { TeamInfoModel } from '../models/TeamInfoModel';
import type { UpdateUserInfoModel } from '../models/UpdateUserInfoModel';

import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class AdminService {

    /**
     * 获取全部用户
     * 使用此接口获取全部用户，需要Admin权限
     * @param count
     * @param skip
     * @returns BasicUserInfoModel 用户列表
     * @throws ApiError
     */
    public static adminUsers(
        count: number = 100,
        skip?: number,
    ): CancelablePromise<Array<BasicUserInfoModel>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Admin/Users',
            query: {
                'count': count,
                'skip': skip,
            },
            errors: {
                401: `未授权用户`,
                403: `禁止访问`,
            },
        });
    }

    /**
     * 获取全部队伍信息
     * 使用此接口获取全部队伍，需要Admin权限
     * @param count
     * @param skip
     * @returns TeamInfoModel 用户列表
     * @throws ApiError
     */
    public static adminTeams(
        count: number = 100,
        skip?: number,
    ): CancelablePromise<Array<TeamInfoModel>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Admin/Teams',
            query: {
                'count': count,
                'skip': skip,
            },
            errors: {
                401: `未授权用户`,
                403: `禁止访问`,
            },
        });
    }

    /**
     * 修改用户信息
     * 使用此接口修改用户信息，需要Admin权限
     * @param userid
     * @param requestBody
     * @returns any 成功更新
     * @throws ApiError
     */
    public static adminUpdateUserInfo(
        userid: string,
        requestBody: UpdateUserInfoModel,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/Admin/Users/{userid}',
            path: {
                'userid': userid,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                401: `未授权用户`,
                403: `禁止访问`,
                404: `用户未找到`,
            },
        });
    }

    /**
     * 获取用户信息
     * 使用此接口获取用户信息，需要Admin权限
     * @param userid
     * @returns ClientUserInfoModel 用户对象
     * @throws ApiError
     */
    public static adminUserInfo(
        userid: string,
    ): CancelablePromise<ClientUserInfoModel> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Admin/Users/{userid}',
            path: {
                'userid': userid,
            },
            errors: {
                401: `未授权用户`,
                403: `禁止访问`,
            },
        });
    }

    /**
     * 删除用户
     * 使用此接口删除用户，需要Admin权限
     * @param userid
     * @returns any 用户对象
     * @throws ApiError
     */
    public static adminDeleteUser(
        userid: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/Admin/Users/{userid}',
            path: {
                'userid': userid,
            },
            errors: {
                401: `未授权用户`,
                403: `禁止访问`,
            },
        });
    }

    /**
     * 获取全部日志
     * 使用此接口获取全部日志，需要Admin权限
     * @param level
     * @param count
     * @param skip
     * @returns ClientUserInfoModel 日志列表
     * @throws ApiError
     */
    public static adminLogs(
        level: string | null = 'All',
        count: number = 50,
        skip?: number,
    ): CancelablePromise<Array<ClientUserInfoModel>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Admin/Logs/{level}',
            path: {
                'level': level,
            },
            query: {
                'count': count,
                'skip': skip,
            },
            errors: {
                401: `未授权用户`,
                403: `禁止访问`,
            },
        });
    }

    /**
     * 获取全部文件
     * 使用此接口获取全部日志，需要Admin权限
     * @param count
     * @param skip
     * @returns LocalFile 日志列表
     * @throws ApiError
     */
    public static adminFiles(
        count: number = 50,
        skip?: number,
    ): CancelablePromise<Array<LocalFile>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Admin/Files',
            query: {
                'count': count,
                'skip': skip,
            },
            errors: {
                401: `未授权用户`,
                403: `禁止访问`,
            },
        });
    }

}