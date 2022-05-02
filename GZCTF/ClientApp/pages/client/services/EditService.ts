/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { Challenge } from '../models/Challenge';
import type { ChallengeInfoModel } from '../models/ChallengeInfoModel';
import type { ChallengeModel } from '../models/ChallengeModel';
import type { FlagInfoModel } from '../models/FlagInfoModel';
import type { Game } from '../models/Game';
import type { GameInfoModel } from '../models/GameInfoModel';
import type { GameNotice } from '../models/GameNotice';
import type { GameNoticeModel } from '../models/GameNoticeModel';
import type { Notice } from '../models/Notice';
import type { NoticeModel } from '../models/NoticeModel';
import type { TaskStatus } from '../models/TaskStatus';

import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class EditService {

    /**
     * 添加公告
     * 添加公告，需要管理员权限
     * @param requestBody
     * @returns Notice 成功添加公告
     * @throws ApiError
     */
    public static editAddNotice(
        requestBody: NoticeModel,
    ): CancelablePromise<Notice> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/edit/notices',
            body: requestBody,
            mediaType: 'application/json',
        });
    }

    /**
     * 获取所有公告
     * 获取所有公告，需要管理员权限
     * @returns Notice 成功获取文件
     * @throws ApiError
     */
    public static editGetNotices(): CancelablePromise<Array<Notice>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/edit/notices',
        });
    }

    /**
     * 修改公告
     * 修改公告，需要管理员权限
     * @param id 公告Id
     * @param requestBody
     * @returns Notice 成功修改公告
     * @throws ApiError
     */
    public static editUpdateNotice(
        id: number,
        requestBody: NoticeModel,
    ): CancelablePromise<Notice> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/edit/notices/{id}',
            path: {
                'id': id,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                404: `未找到公告`,
            },
        });
    }

    /**
     * 删除公告
     * 删除公告，需要管理员权限
     * @param id 公告Id
     * @returns any 成功删除公告
     * @throws ApiError
     */
    public static editDeleteNotice(
        id: number,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/edit/notices/{id}',
            path: {
                'id': id,
            },
            errors: {
                404: `未找到公告`,
            },
        });
    }

    /**
     * 添加比赛
     * 添加比赛，需要管理员权限
     * @param requestBody
     * @returns Game 成功获取文件
     * @throws ApiError
     */
    public static editAddGame(
        requestBody: GameInfoModel,
    ): CancelablePromise<Game> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/edit/games',
            body: requestBody,
            mediaType: 'application/json',
        });
    }

    /**
     * 获取比赛列表
     * 获取比赛列表，需要管理员权限
     * @param count
     * @param skip
     * @returns GameInfoModel 成功获取文件
     * @throws ApiError
     */
    public static editGetGames(
        count?: number,
        skip?: number,
    ): CancelablePromise<Array<GameInfoModel>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/edit/games',
            query: {
                'count': count,
                'skip': skip,
            },
        });
    }

    /**
     * 获取比赛
     * 获取比赛，需要管理员权限
     * @param id
     * @returns Game 成功获取文件
     * @throws ApiError
     */
    public static editGetGame(
        id: number,
    ): CancelablePromise<Game> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/edit/games/{id}',
            path: {
                'id': id,
            },
        });
    }

    /**
     * 修改比赛
     * 修改比赛，需要管理员权限
     * @param id
     * @param requestBody
     * @returns Game 成功获取文件
     * @throws ApiError
     */
    public static editUpdateGame(
        id: number,
        requestBody: GameInfoModel,
    ): CancelablePromise<Game> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/edit/games/{id}',
            path: {
                'id': id,
            },
            body: requestBody,
            mediaType: 'application/json',
        });
    }

    /**
     * 添加比赛公告
     * 添加比赛公告，需要管理员权限
     * @param id 比赛ID
     * @param requestBody
     * @returns GameNotice 成功添加比赛公告
     * @throws ApiError
     */
    public static editAddGameNotice(
        id: number,
        requestBody: GameNoticeModel,
    ): CancelablePromise<GameNotice> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/edit/games/{id}/notices',
            path: {
                'id': id,
            },
            body: requestBody,
            mediaType: 'application/json',
        });
    }

    /**
     * 获取比赛公告
     * 获取比赛公告，需要管理员权限
     * @param id 比赛ID
     * @param count 数量
     * @param skip 跳过数量
     * @returns GameNotice 成功获取文件
     * @throws ApiError
     */
    public static editGetGameNotices(
        id: number,
        count?: number,
        skip?: number,
    ): CancelablePromise<GameNotice> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/edit/games/{id}/notices',
            path: {
                'id': id,
            },
            query: {
                'count': count,
                'skip': skip,
            },
        });
    }

    /**
     * 删除比赛公告
     * 删除比赛公告，需要管理员权限
     * @param id 比赛ID
     * @param noticeId 公告Id
     * @returns any 成功删除公告
     * @throws ApiError
     */
    public static editDeleteGameNotice(
        id: number,
        noticeId: number,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/edit/games/{id}/notices/{noticeId}',
            path: {
                'id': id,
                'noticeId': noticeId,
            },
            errors: {
                404: `未找到公告`,
            },
        });
    }

    /**
     * 添加比赛题目
     * 添加比赛题目，需要管理员权限
     * @param id 比赛ID
     * @param requestBody
     * @returns Challenge 成功添加比赛题目
     * @throws ApiError
     */
    public static editAddGameChallenge(
        id: number,
        requestBody: ChallengeModel,
    ): CancelablePromise<Challenge> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/edit/games/{id}/challenges',
            path: {
                'id': id,
            },
            body: requestBody,
            mediaType: 'application/json',
        });
    }

    /**
     * 获取全部比赛题目
     * 获取全部比赛题目，需要管理员权限
     * @param id 比赛ID
     * @param count 数量
     * @param skip 跳过数量
     * @returns ChallengeInfoModel 成功获取比赛题目
     * @throws ApiError
     */
    public static editGetGameChallenges(
        id: number,
        count?: number,
        skip?: number,
    ): CancelablePromise<Array<ChallengeInfoModel>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/edit/games/{id}/challenges',
            path: {
                'id': id,
            },
            query: {
                'count': count,
                'skip': skip,
            },
        });
    }

    /**
     * 获取比赛题目
     * 获取比赛题目，需要管理员权限
     * @param id 比赛ID
     * @param cId 题目Id
     * @returns Challenge 成功添加比赛题目
     * @throws ApiError
     */
    public static editAddGameChallenge2(
        id: number,
        cId: number,
    ): CancelablePromise<Challenge> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/edit/games/{id}/challenges/{cId}',
            path: {
                'id': id,
                'cId': cId,
            },
        });
    }

    /**
     * 修改比赛题目信息
     * 修改比赛题目，需要管理员权限
     * @param id 比赛ID
     * @param cId 题目Id
     * @param requestBody 题目信息
     * @returns Challenge 成功添加比赛题目
     * @throws ApiError
     */
    public static editUpdateGameChallenge(
        id: number,
        cId: number,
        requestBody: ChallengeModel,
    ): CancelablePromise<Challenge> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/edit/games/{id}/challenges/{cId}',
            path: {
                'id': id,
                'cId': cId,
            },
            body: requestBody,
            mediaType: 'application/json',
        });
    }

    /**
     * 删除比赛题目
     * 删除比赛题目，需要管理员权限
     * @param id 比赛ID
     * @param cId 题目Id
     * @returns any 成功添加比赛题目
     * @throws ApiError
     */
    public static editRemoveGameChallenge(
        id: number,
        cId: number,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/edit/games/{id}/challenges/{cId}',
            path: {
                'id': id,
                'cId': cId,
            },
        });
    }

    /**
     * 添加比赛题目 Flag
     * 添加比赛题目 Flag，需要管理员权限
     * @param id 比赛ID
     * @param cId 题目ID
     * @param requestBody
     * @returns number 成功添加比赛题目
     * @throws ApiError
     */
    public static editAddFlag(
        id: number,
        cId: number,
        requestBody: FlagInfoModel,
    ): CancelablePromise<number> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/edit/games/{id}/challenges/{cId}/flags',
            path: {
                'id': id,
                'cId': cId,
            },
            body: requestBody,
            mediaType: 'application/json',
        });
    }

    /**
     * 删除比赛题目 Flag
     * 删除比赛题目 Flag，需要管理员权限
     * @param id 比赛ID
     * @param cId 题目ID
     * @param fId Flag ID
     * @returns TaskStatus 成功添加比赛题目
     * @throws ApiError
     */
    public static editRemoveFlag(
        id: number,
        cId: number,
        fId: number,
    ): CancelablePromise<TaskStatus> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/edit/games/{id}/challenges/{cId}/flags/{fId}',
            path: {
                'id': id,
                'cId': cId,
                'fId': fId,
            },
        });
    }

}