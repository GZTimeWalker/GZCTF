/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { TeamInfoModel } from '../models/TeamInfoModel';
import type { TeamUpdateModel } from '../models/TeamUpdateModel';

import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class TeamService {

    /**
     * 获取队伍信息
     * 根据 id 获取一个队伍的基本信息
     * @param id 队伍id
     * @returns TeamInfoModel 成功获取队伍信息
     * @throws ApiError
     */
    public static teamGetBasicInfo(
        id: number,
    ): CancelablePromise<TeamInfoModel> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Team/{id}',
            path: {
                'id': id,
            },
            errors: {
                400: `队伍不存在`,
            },
        });
    }

    /**
     * 更改队伍信息
     * 队伍信息更改接口，需要为队伍创建者
     * @param id 队伍Id
     * @param requestBody
     * @returns TeamInfoModel 成功获取队伍信息
     * @throws ApiError
     */
    public static teamUpdateTeam(
        id: number,
        requestBody: TeamUpdateModel,
    ): CancelablePromise<TeamInfoModel> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/Team/{id}',
            path: {
                'id': id,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `队伍不存在`,
                401: `未授权`,
                403: `无权操作`,
            },
        });
    }

    /**
     * 删除队伍
     * 用户删除队伍接口，需要User权限，且为队伍队长
     * @param id 队伍Id
     * @param requestBody 队伍Id
     * @returns TeamInfoModel 成功获取队伍信息
     * @throws ApiError
     */
    public static teamDeleteTeam(
        id: string,
        requestBody: number,
    ): CancelablePromise<TeamInfoModel> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/Team/{id}',
            path: {
                'id': id,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `队伍不存在`,
            },
        });
    }

    /**
     * 获取当前自己队伍信息
     * 根据用户获取一个队伍的基本信息
     * @returns TeamInfoModel 成功获取队伍信息
     * @throws ApiError
     */
    public static teamGetTeamsInfo(): CancelablePromise<Array<TeamInfoModel>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Team',
            errors: {
                400: `队伍不存在`,
            },
        });
    }

    /**
     * 创建队伍
     * 用户创建队伍接口，每个用户只能创建一个队伍
     * @param requestBody
     * @returns TeamInfoModel 成功获取队伍信息
     * @throws ApiError
     */
    public static teamCreateTeam(
        requestBody: TeamUpdateModel,
    ): CancelablePromise<TeamInfoModel> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/Team',
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                400: `队伍不存在`,
            },
        });
    }

    /**
     * 设置队伍为当前激活队伍
     * 设置队伍为当前激活队伍接口，需要为用户
     * @param id 队伍Id
     * @returns TeamInfoModel 成功获取队伍信息
     * @throws ApiError
     */
    public static teamSetActive(
        id: number,
    ): CancelablePromise<TeamInfoModel> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/Team/{id}/SetActive',
            path: {
                'id': id,
            },
            errors: {
                400: `队伍不存在`,
                401: `未授权`,
                403: `无权操作`,
            },
        });
    }

    /**
     * 获取邀请信息
     * 获取队伍邀请信息，需要为队伍创建者
     * @param id 队伍Id
     * @returns string 成功获取队伍Token
     * @throws ApiError
     */
    public static teamTeamInviteToken(
        id: number,
    ): CancelablePromise<string> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/Team/{id}/Invite',
            path: {
                'id': id,
            },
            errors: {
                400: `队伍不存在`,
                401: `未授权`,
                403: `无权操作`,
            },
        });
    }

    /**
     * 更新邀请 Token
     * 更新邀请 Token 的接口，需要为队伍创建者
     * @param id 队伍Id
     * @returns string 成功获取队伍Token
     * @throws ApiError
     */
    public static teamUpdateInviteToken(
        id: number,
    ): CancelablePromise<string> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/Team/{id}/UpdateInviteToken',
            path: {
                'id': id,
            },
            errors: {
                400: `队伍不存在`,
                401: `未授权`,
                403: `无权操作`,
            },
        });
    }

    /**
     * 踢除队伍接口
     * 踢除用户接口，踢出对应id的用户，需要队伍创建者权限
     * @param id 队伍Id
     * @param userid 被踢除用户Id
     * @returns string 成功获取队伍Token
     * @throws ApiError
     */
    public static teamKickUser(
        id: number,
        userid: string,
    ): CancelablePromise<string> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/Team/{id}/Kick/{userid}',
            path: {
                'id': id,
                'userid': userid,
            },
            errors: {
                400: `队伍不存在`,
                401: `未授权`,
                403: `无权操作`,
            },
        });
    }

    /**
     * 接受邀请
     * 接受邀请的接口，需要User权限，且不在队伍中
     * @param id 队伍Id
     * @param token 队伍邀请Token
     * @returns any 接受队伍邀请
     * @throws ApiError
     */
    public static teamAccept(
        id: number,
        token: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/Team/{id}/Accept/{token}',
            path: {
                'id': id,
                'token': token,
            },
            errors: {
                400: `队伍不存在`,
                401: `未授权`,
                403: `无权操作`,
            },
        });
    }

    /**
     * 离开队伍
     * 离开队伍的接口，需要User权限，且在队伍中
     * @param id 队伍Id
     * @returns any 成功离开队伍
     * @throws ApiError
     */
    public static teamLeave(
        id: number,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/Team/{id}/Leave',
            path: {
                'id': id,
            },
            errors: {
                400: `队伍不存在`,
                401: `未授权`,
                403: `无权操作`,
            },
        });
    }

    /**
     * 更新队伍头像接口
     * 使用此接口更新队伍头像，需要User权限，且为队伍成员
     * @param id
     * @param formData
     * @returns string 用户头像URL
     * @throws ApiError
     */
    public static teamAvatar(
        id: number,
        formData?: {
            file?: Blob;
        },
    ): CancelablePromise<string> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/Team/{id}/Avatar',
            path: {
                'id': id,
            },
            formData: formData,
            mediaType: 'multipart/form-data',
            errors: {
                400: `非法请求`,
                401: `未授权用户`,
            },
        });
    }

}