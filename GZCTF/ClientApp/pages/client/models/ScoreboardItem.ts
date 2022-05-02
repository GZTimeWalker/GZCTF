/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { ChallengeItem } from './ChallengeItem';

export type ScoreboardItem = {
    /**
     * 队伍Id
     */
    id?: number;
    /**
     * 队伍名称
     */
    name?: string;
    /**
     * 队伍头像
     */
    avatar?: string | null;
    /**
     * 分数
     */
    score?: number;
    /**
     * 排名
     */
    rank?: number;
    /**
     * 已解出的题目数量
     */
    solvedCount?: number;
    /**
     * 题目情况列表
     */
    challenges?: Array<ChallengeItem>;
};
