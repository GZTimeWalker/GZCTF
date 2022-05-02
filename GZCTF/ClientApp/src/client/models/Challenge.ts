/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { ChallengeTag } from './ChallengeTag';
import type { ChallengeType } from './ChallengeType';
import type { FlagContext } from './FlagContext';
import type { Game } from './Game';
import type { Submission } from './Submission';

export type Challenge = {
    id?: number;
    /**
     * 题目名称
     */
    title: string;
    /**
     * 题目内容
     */
    content: string;
    /**
     * 是否启用题目
     */
    isEnabled?: boolean;
    /**
     * 题目标签
     */
    tag?: ChallengeTag;
    /**
     * 题目提示，用";"分隔
     */
    hints?: string;
    /**
     * 初始分数
     */
    originalScore: number;
    /**
     * 最低分数
     */
    minScore: number;
    /**
     * 预期最大解出人数
     */
    expectMaxCount: number;
    /**
     * 奖励人数
     */
    awardCount: number;
    /**
     * 题目类型
     */
    type: ChallengeType;
    /**
     * 下载文件名称
     */
    fileName?: string;
    /**
     * 当前题目分值
     */
    currentScore?: number;
    flags?: Array<FlagContext>;
    /**
     * 提交
     */
    submissions?: Array<Submission>;
    /**
     * 比赛对象
     */
    game?: Game;
    gameId?: number;
};
