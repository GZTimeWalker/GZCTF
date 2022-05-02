/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { AnswerResult } from '../models/AnswerResult';
import type { BasicGameInfoModel } from '../models/BasicGameInfoModel';
import type { ChallengeDetailModel } from '../models/ChallengeDetailModel';
import type { ChallengeInfo } from '../models/ChallengeInfo';
import type { GameDetailsModel } from '../models/GameDetailsModel';
import type { GameEvent } from '../models/GameEvent';
import type { InstanceInfoModel } from '../models/InstanceInfoModel';
import type { ScoreboardModel } from '../models/ScoreboardModel';
import type { Submission } from '../models/Submission';

import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class GameService {

    /**
     * 获取最新的比赛
     * 获取最近十个比赛
     * @returns BasicGameInfoModel 成功获取比赛信息
     * @throws ApiError
     */
    public static gameGamesAll(): CancelablePromise<Array<BasicGameInfoModel>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Game',
        });
    }

    /**
     * 获取比赛详细信息
     * 获取比赛的详细信息
     * @param id 比赛id
     * @returns GameDetailsModel 成功获取比赛信息
     * @throws ApiError
     */
    public static gameGames(
        id: number,
    ): CancelablePromise<GameDetailsModel> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Game/{id}',
            path: {
                'id': id,
            },
        });
    }

    /**
     * 加入一个比赛
     * 加入一场比赛，需要User权限，需要当前激活队伍的队长权限
     * @param id 比赛id
     * @returns any 成功获取比赛信息
     * @throws ApiError
     */
    public static gameJoinGame(
        id: number,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/Game/{id}',
            path: {
                'id': id,
            },
        });
    }

    /**
     * 获取积分榜
     * 获取积分榜数据
     * @param id 比赛id
     * @returns ScoreboardModel 成功获取比赛信息
     * @throws ApiError
     */
    public static gameScoreboard(
        id: number,
    ): CancelablePromise<ScoreboardModel> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Game/{id}/Scoreboard',
            path: {
                'id': id,
            },
        });
    }

    /**
     * 获取比赛事件
     * 获取比赛事件数据
     * @param id 比赛id
     * @returns GameEvent 成功获取比赛事件
     * @throws ApiError
     */
    public static gameNotices(
        id: number,
    ): CancelablePromise<Array<GameEvent>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Game/{id}/Notices',
            path: {
                'id': id,
            },
        });
    }

    /**
     * 获取比赛提交
     * 获取比赛提交数据，需要观察者权限
     * @param id 比赛id
     * @param count
     * @param skip
     * @returns Submission 成功获取比赛提交
     * @throws ApiError
     */
    public static gameSubmissions(
        id: number,
        count: number = 100,
        skip?: number,
    ): CancelablePromise<Array<Submission>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Game/{id}/Submissions',
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
     * 获取比赛实例列表
     * 获取比赛实例数据，需要观察者权限
     * @param id 比赛id
     * @param count
     * @param skip
     * @returns InstanceInfoModel 成功获取比赛提交
     * @throws ApiError
     */
    public static gameInstances(
        id: number,
        count: number = 100,
        skip?: number,
    ): CancelablePromise<Array<InstanceInfoModel>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Game/{id}/Instances',
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
     * 获取全部比赛题目信息
     * 获取比赛的全部题目，需要User权限，需要当前激活队伍已经报名
     * @param id 比赛id
     * @returns ChallengeInfo 成功获取比赛题目信息
     * @throws ApiError
     */
    public static gameChallenges(
        id: number,
    ): CancelablePromise<Record<string, Array<ChallengeInfo>>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Game/{id}/Challenges',
            path: {
                'id': id,
            },
        });
    }

    /**
     * 获取比赛题目信息
     * 获取比赛题目信息，需要User权限，需要当前激活队伍已经报名
     * @param id 比赛id
     * @param challengeId 题目id
     * @returns ChallengeDetailModel 成功获取比赛题目信息
     * @throws ApiError
     */
    public static gameGetChallenge(
        id: number,
        challengeId: number,
    ): CancelablePromise<ChallengeDetailModel> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Game/{id}/Challenges/{challengeId}',
            path: {
                'id': id,
                'challengeId': challengeId,
            },
        });
    }

    /**
     * 提交 flag
     * 提交flag，需要User权限，需要当前激活队伍已经报名
     * @param id 比赛id
     * @param challengeId 题目id
     * @param requestBody 提交Flag
     * @returns any 成功获取比赛题目信息
     * @throws ApiError
     */
    public static gameSubmit(
        id: number,
        challengeId: number,
        requestBody: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/Game/{id}/Challenges/{challengeId}',
            path: {
                'id': id,
                'challengeId': challengeId,
            },
            body: requestBody,
            mediaType: 'application/json',
        });
    }

    /**
     * 查询 flag 状态
     * 查询 flag 状态，需要User权限
     * @param id 比赛id
     * @param challengeId 题目id
     * @param submitId 提交id
     * @returns AnswerResult 成功获取比赛题目信息
     * @throws ApiError
     */
    public static gameStatus(
        id: number,
        challengeId: number,
        submitId: number,
    ): CancelablePromise<AnswerResult> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Game/{id}/Status/{submitId}',
            path: {
                'id': id,
                'challengeId': challengeId,
                'submitId': submitId,
            },
        });
    }

}