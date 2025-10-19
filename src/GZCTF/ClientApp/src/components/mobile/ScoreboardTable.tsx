import { Avatar, Box, Group, Pagination, Paper, Select, Stack, Table, useMantineTheme } from '@mantine/core'
import cx from 'clsx'
import React, { FC, useEffect, useState, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { ScoreboardProps } from '@Components/ScoreboardTable'
import { ScrollingText } from '@Components/ScrollingText'
import { MobileScoreboardItemModal } from '@Components/mobile/ScoreboardItemModal'
import { BloodBonus, useBonusLabels } from '@Utils/Shared'
import { useGameScoreboard } from '@Hooks/useGame'
import { ScoreboardItem } from '@Api'
import misc from '@Styles/Misc.module.css'
import classes from '@Styles/ScoreboardTable.module.css'

const RANK_WIDTH = 50
const SCORE_WIDTH = 70

const TableRow: FC<{
  item: ScoreboardItem
  onOpenDetail: () => void
}> = React.memo(({ item, onOpenDetail }) => {
  const theme = useMantineTheme()
  const solved = item.solvedChallenges

  const totalScore = useMemo(() => {
    return solved?.reduce((acc, cur) => acc + (cur?.score ?? 0), 0) ?? 0
  }, [solved])

  return (
    <Table.Tr>
      <Table.Td className={cx(classes.mono, classes.left)}>{item.rank || '-'}</Table.Td>
      <Table.Td className={cx(classes.left, classes.teamCell)}>
        <Group justify="left" gap={5} wrap="nowrap" style={{ minWidth: 0, flex: 1 }}>
          <Avatar
            alt="avatar"
            src={item.avatar}
            radius="xl"
            size={30}
            color={theme.primaryColor}
            className={misc.cPointer}
          >
            {item.name?.slice(0, 1) ?? 'T'}
          </Avatar>
          <ScrollingText
            text={item.name || ''}
            size="sm"
            onClick={onOpenDetail}
            style={{ width: '100%', minWidth: 0 }}
          />
        </Group>
      </Table.Td>
      <Table.Td className={cx(classes.mono, classes.left)}>{totalScore}</Table.Td>
    </Table.Tr>
  )
})

const ITEM_COUNT_PER_PAGE = 10

export const MobileScoreboardTable: FC<ScoreboardProps> = ({ divisionId, setDivisionId }) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const [activePage, setPage] = useState(1)
  const [bloodBonus, setBloodBonus] = useState(BloodBonus.default)

  const { scoreboard } = useGameScoreboard(numId)

  const divisionOptions = useMemo(
    () =>
      (scoreboard?.divisions ?? [])
        .filter((div) => div.id !== undefined && div.id !== null)
        .map((div) => ({
          value: div.id!.toString(),
          label: div.name?.trim() || `#${div.id}`,
        })),
    [scoreboard?.divisions]
  )

  const selectValue = useMemo(() => (divisionId === null ? 'all' : divisionId.toString()), [divisionId])

  useEffect(() => {
    if (divisionId !== null && !divisionOptions.some((opt) => Number(opt.value) === divisionId)) {
      setDivisionId(null)
    }
  }, [divisionOptions, divisionId, setDivisionId])

  const filtered = useMemo(() => {
    if (divisionId === null) return scoreboard?.items
    return scoreboard?.items?.filter((s) => (s.divisionId ?? null) === divisionId)
  }, [scoreboard, divisionId])

  const base = (activePage - 1) * ITEM_COUNT_PER_PAGE
  const currentItems = filtered?.slice(base, base + ITEM_COUNT_PER_PAGE)

  const [currentItem, setCurrentItem] = useState<ScoreboardItem | null>(null)
  const [itemDetailOpened, setItemDetailOpened] = useState(false)

  const { t } = useTranslation()

  useEffect(() => {
    if (scoreboard) {
      setBloodBonus(new BloodBonus(scoreboard.bloodBonus))
    }
  }, [scoreboard])

  const divisionMap = useMemo(() => {
    const map = new Map<number, string>()
    scoreboard?.divisions?.forEach((div) => {
      map.set(div.id, div.name.trim())
    })
    return map
  }, [scoreboard?.divisions])

  const bloodData = useBonusLabels(bloodBonus)

  return (
    <Paper shadow="xs" p="sm">
      <Stack gap="xs">
        {divisionOptions.length > 0 && (
          <Select
            defaultValue="all"
            data={[{ value: 'all', label: t('game.label.score_table.all_teams') }, ...divisionOptions]}
            value={selectValue}
            onChange={(div) => {
              if (!div || div === 'all') {
                setDivisionId(null)
              } else {
                const parsed = Number(div)
                setDivisionId(Number.isNaN(parsed) ? null : parsed)
              }
              setPage(1)
            }}
          />
        )}
        <Box
          pos="relative"
          maw="100%"
          style={{
            overflow: 'scroll',
            '&::-webkit-scrollbar': {
              display: 'none',
            },
          }}
        >
          <Table className={classes.table}>
            <colgroup>
              <col style={{ width: RANK_WIDTH }} />
              <col />
              <col style={{ width: SCORE_WIDTH }} />
            </colgroup>
            <Table.Thead className={classes.thead}>
              <Table.Tr>
                <Table.Th className={cx(classes.left, classes.theadHeader)}>
                  {t('game.label.score_table.rank_total')}
                </Table.Th>
                <Table.Th className={cx(classes.left, classes.theadHeader)}>
                  {t('game.label.score_table.team')}
                </Table.Th>
                <Table.Th className={cx(classes.left, classes.theadHeader)}>
                  {t('game.label.score_table.score_total')}
                </Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {scoreboard &&
                currentItems?.map((item, idx) => (
                  <TableRow
                    key={base + idx}
                    item={item}
                    onOpenDetail={() => {
                      setCurrentItem(item)
                      setItemDetailOpened(true)
                    }}
                  />
                ))}
            </Table.Tbody>
          </Table>
        </Box>
        <Group justify="center" wrap="nowrap">
          <Pagination
            size="sm"
            value={activePage}
            onChange={setPage}
            total={Math.ceil((filtered?.length ?? 1) / ITEM_COUNT_PER_PAGE)}
          />
        </Group>
      </Stack>
      <MobileScoreboardItemModal
        scoreboard={scoreboard}
        divisionMap={divisionMap}
        bloodBonusMap={bloodData}
        opened={itemDetailOpened}
        withCloseButton={false}
        size="40rem"
        onClose={() => setItemDetailOpened(false)}
        item={currentItem}
      />
    </Paper>
  )
}
