/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { ContainerStatus } from './ContainerStatus';

export type ContainerInfoModel = {
    /**
     * 容器状态
     */
    status?: ContainerStatus;
    /**
     * 容器创建时间
     */
    startedAt?: string;
    /**
     * 容器期望终止时间
     */
    expectStopAt?: string;
    /**
     * 题目入口
     */
    entry?: string;
};
