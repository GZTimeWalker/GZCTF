/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { ChallengeInfoModel } from './ChallengeInfoModel';
import type { ContainerInfoModel } from './ContainerInfoModel';

export type InstanceInfoModel = {
    /**
     * 实例 Id
     */
    id?: number;
    /**
     * 队伍 Id
     */
    teamId?: number;
    /**
     * 队伍名
     */
    teamName?: string;
    /**
     * 题目详情
     */
    challenge?: ChallengeInfoModel;
    /**
     * 容器信息
     */
    container?: ContainerInfoModel | null;
};
