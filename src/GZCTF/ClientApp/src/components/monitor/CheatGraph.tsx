import { useMantineTheme } from '@mantine/core'
import { EchartsContainer } from '@Components/charts/EchartsContainer'
import type { CheatReport, IpAnalysisResult, SequenceSuspectResult } from '@Api'
import { FC, useMemo } from 'react'

interface CheatGraphProps {
    report: CheatReport | null
}

export const CheatGraph: FC<CheatGraphProps> = ({ report }) => {
    const theme = useMantineTheme()

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

        // Ensure teams discussed in Sequence Suspects are nodes too (if not already via IP Issue)
        // Note: mapping TeamName to ID is hard if we only have names in SequenceSuspects
        // Check Api.ts: SequenceSuspectResult now has teamA (string), teamB (string). Not IDs.
        // Ideally we match by Name.
        // Let's iterate sequence suspects.
        report.sequenceSuspects?.forEach(item => {
            // We can't easily link to 'team:ID' nodes if we only have names.
            // But IpAnalysis has teamName. We can try to finding existing nodes by name.
            // Or create new nodes by name if not found.

            // Simpler: Just link nodes by NAME if we use Name as ID? 
            // No, IDs are safer. 
            // Let's assume SequenceSuspects usually involves teams already in IP Analysis (often correlated).
            // If not, we might miss them or create duplicate nodes. 
            // But for visualization, let's try to match by Name if ID unknown.

            ['A', 'B'].forEach(suffix => {
                const tName = suffix === 'A' ? item.teamA : item.teamB;
                if (!tName) return;

                // Check if node exists by checking if any node.name == tName
                if (!nodes.find(n => n.name === tName)) {
                    nodes.push({
                        id: `team:name:${tName}`,
                        name: tName,
                        value: tName,
                        symbolSize: 20,
                        category: 0,
                        itemStyle: { color: theme.colors.blue[6] }
                    });
                }
            });

            const sourceId = nodes.find(n => n.name === item.teamA)?.id;
            const targetId = nodes.find(n => n.name === item.teamB)?.id;

            if (sourceId && targetId) {
                links.push({
                    source: sourceId,
                    target: targetId,
                    value: `Similarity: ${(item.similarity! * 100).toFixed(0)}%`,
                    lineStyle: {
                        type: 'dashed',
                        color: theme.colors.orange[6],
                        width: 2
                    }
                })
            }
        });


        return {
            tooltip: {},
            legend: [{
                data: ['Team', 'IP']
            }],
            series: [
                {
                    type: 'graph' as const,
                    layout: 'force' as const,
                    data: nodes,
                    links: links,
                    categories: [{ name: 'Team' }, { name: 'IP' }],
                    roam: true,
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
            ]
        }
    }, [report, theme])

    if (!report || (!report.ipAnalysis?.length && !report.sequenceSuspects?.length)) {
        return null; // Or show "No Graph Data"
    }

    return (
        <EchartsContainer
            option={option}
            style={{ height: '500px', width: '100%' }}
        />
    )
}
