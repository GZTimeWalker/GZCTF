import { ActionIcon, useMantineTheme } from '@mantine/core'
import { EchartsContainer } from '@Components/charts/EchartsContainer'
import type { CheatReport, IpAnalysisResult } from '@Api'
import { FC, useMemo, useCallback } from 'react'
import { notifications } from '@mantine/notifications'
import { useFullscreen } from '@mantine/hooks'
import Icon from '@mdi/react'
import { mdiCheck, mdiFullscreen, mdiFullscreenExit } from '@mdi/js'
import copy from 'copy-to-clipboard'

interface CheatGraphProps {
    report: CheatReport | null
}

export const CheatGraph: FC<CheatGraphProps> = ({ report }) => {
    const theme = useMantineTheme()
    const { ref, toggle, fullscreen } = useFullscreen()

    const onChartClick = useCallback((params: any) => {
        if (params.dataType === 'node' && params.name) {
            copy(params.name)
            notifications.show({
                title: 'Copied',
                message: `Copied ${params.name} to clipboard`,
                color: 'green',
                icon: <Icon path={mdiCheck} size={0.7} />
            })
        } else if (params.dataType === 'edge' && params.value) {
            copy(params.value.toString())
            notifications.show({
                title: 'Copied',
                message: `Copied relation info to clipboard`,
                color: 'green',
                icon: <Icon path={mdiCheck} size={0.7} />
            })
        }
    }, [])

    const option = useMemo(() => {
        if (!report) return {}

        const nodes: any[] = []
        const links: any[] = []
        const teamIds = new Set<number>()
        const ips = new Set<string>()

        // Process IP Analysis for Team-IP links
        report.ipAnalysis?.forEach((item) => {
            if (!item.teamId || !item.ip) return

            // Add Team Node if new
            if (!teamIds.has(item.teamId)) {
                teamIds.add(item.teamId)
                nodes.push({
                    id: `team:${item.teamId}`,
                    name: item.teamName,
                    value: item.teamName,
                    symbolSize: 20,
                    category: 0,
                    itemStyle: { color: theme.colors.blue[6] }
                })
            }

            // Add IP Node if new
            if (!ips.has(item.ip)) {
                ips.add(item.ip)
                nodes.push({
                    id: `ip:${item.ip}`,
                    name: item.ip,
                    value: item.ip,
                    symbolSize: 15,
                    category: 1,
                    itemStyle: { color: theme.colors.gray[6] },
                    label: { show: true, position: 'right' }
                })
            }

            // Add Link
            links.push({
                source: `team:${item.teamId}`,
                target: `ip:${item.ip}`,
                value: item.type,
                lineStyle: {
                    color: item.type === 'CrossTeamIP' ? theme.colors.red[6] :
                        item.type === 'UnknownIP' ? theme.colors.grape[6] : theme.colors.gray[4],
                    width: 2
                }
            })
        })




        return {
            tooltip: {
                trigger: 'item' as const,
                enterable: true,
                appendToBody: true,
                confine: true,
                extraCssText: 'user-select: text; pointer-events: auto;'
            },
            legend: [{}],
            series: [
                {
                    type: 'graph' as const,
                    layout: 'force' as const,
                    clip: true,
                    data: nodes,
                    links: links,
                    categories: [{ name: 'Team' }, { name: 'IP' }],
                    roam: true,
                    draggable: true,
                    label: {
                        show: true,
                        position: 'right' as const,
                        formatter: '{b}'
                    },
                    force: {
                        repulsion: 300,
                        edgeLength: 100
                    },
                    lineStyle: {
                        curveness: 0.3
                    }
                }
            ],
            emphasis: {
                focus: 'adjacency',
                itemStyle: {
                    // Keep the invisible border on hover so it doesn't "shrink"
                    borderColor: 'transparent',
                    borderWidth: 10
                }
            },
            itemStyle: {
                color: theme.colors.gray[6],
                borderColor: 'transparent',
                borderWidth: 10
            },
        }
    }, [report, theme])

    if (!report || !report.ipAnalysis?.length) {
        return null;
    }

    return (
        <div ref={ref} style={{ position: 'relative', width: '100%', height: fullscreen ? '100vh' : 'auto', backgroundColor: fullscreen ? theme.colors.dark[7] : undefined }}>
            <ActionIcon
                onClick={toggle}
                style={{ position: 'absolute', top: 10, right: 10, zIndex: 100 }}
                variant="filled"
                color={fullscreen ? 'gray' : 'blue'}
                size="lg"
            >
                <Icon path={fullscreen ? mdiFullscreenExit : mdiFullscreen} size={1} />
            </ActionIcon>
            <EchartsContainer
                option={option}
                style={{ height: fullscreen ? '100%' : '500px', width: '100%' }}
                onEvents={{ click: onChartClick }}
                opts={{
                    renderer: 'canvas',
                }}
            />
        </div>
    )
}
