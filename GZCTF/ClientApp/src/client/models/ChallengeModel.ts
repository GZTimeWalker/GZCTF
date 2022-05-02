/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { ChallengeTag } from './ChallengeTag';
import type { ChallengeType } from './ChallengeType';

export type ChallengeModel = {
    /**
     * 题目名称
     */
    title: string;
    /**
     * 题目内容
     */
    content: string;
    /**
     * 题目标签
     */
    tag?: ChallengeTag;
    /**
     * 题目提示，用";"分隔
     */
    hints?: string;
    /**
     * 题目类型
     */
    type?: ChallengeType;
    /**
     * 镜像名称与标签
     */
    containerImage?: string | null;
    /**
     * 运行内存限制 (MB)
     */
    memoryLimit?: number | null;
    /**
     * CPU 运行数量限制
     */
    cpuCount?: number | null;
    /**
     * 镜像暴露端口
     */
    containerExposePort?: number | null;
    /**
     * 初始分数
     */
    originalScore?: number;
    /**
     * 最低分数
     */
    minScore?: number;
    /**
     * 预期最大解出人数
     */
    expectMaxCount?: number;
    /**
     * 奖励人数
     */
    awardCount?: number;
    /**
     * 统一文件名
     */
    fileName?: string | null;
};
