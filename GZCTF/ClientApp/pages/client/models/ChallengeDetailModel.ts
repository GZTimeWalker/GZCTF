/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { ChallengeTag } from './ChallengeTag';
import type { ChallengeType } from './ChallengeType';
import type { ClientFlagContext } from './ClientFlagContext';

export type ChallengeDetailModel = {
    /**
     * 题目 Id
     */
    id?: number;
    /**
     * 题目名称
     */
    title?: string;
    /**
     * 题目内容
     */
    content?: string;
    /**
     * 题目标签
     */
    tag?: ChallengeTag;
    /**
     * 题目提示，用";"分隔
     */
    hints?: string;
    /**
     * 题目当前分值
     */
    score?: number;
    /**
     * 题目类型
     */
    type?: ChallengeType;
    /**
     * Flag 上下文
     */
    context?: ClientFlagContext;
};
