import { Avatar, Box, Group, Input, Pagination, Paper, Select, Stack, Table, useMantineTheme } from '@mantine/core'
import cx from 'clsx'
import React, { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { ScoreboardProps } from '@Components/ScoreboardTable'
import { MobileScoreboardItemModal } from '@Components/mobile/ScoreboardItemModal'
import { BloodBonus, useBonusLabels } from '@Utils/Shared'
import { useGameScoreboard } from '@Hooks/useGame'
import { ScoreboardItem } from '@Api'
import misc from '@Styles/Misc.module.css'
import classes from '@Styles/ScoreboardTable.module.css'

const TableRow: FC<{
  item: ScoreboardItem
  onOpenDetail: () => void
}> = ({ item, onOpenDetail }) => {
  const theme = useMantineTheme()
  const solved = item.solvedChallenges
  return (
    <Table.Tr>
      <Table.Td className={cx(classes.mono, classes.left)}>{item.rank}</Table.Td>
      <Table.Td className={classes.left}>
        <Group justify="left" gap={5} wrap="nowrap" onClick={onOpenDetail}>
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
          <Input variant="unstyled" value={item.name} readOnly size="sm" />
        </Group>
      </Table.Td>
      <Table.Td className={cx(classes.mono, classes.left)}>
        {solved?.reduce((acc, cur) => acc + (cur?.score ?? 0), 0)}
      </Table.Td>
    </Table.Tr>
  )
}

const ITEM_COUNT_PER_PAGE = 10

export const MobileScoreboardTable: FC<ScoreboardProps> = ({ division, setDivision }) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const [activePage, setPage] = useState(1)
  const [bloodBonus, setBloodBonus] = useState(BloodBonus.default)

  const { scoreboard } = useGameScoreboard(numId)

  const filtered = division === 'all' ? scoreboard?.items : scoreboard?.items?.filter((s) => s.division === division)

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

  const bloodData = useBonusLabels(bloodBonus)

  return (
    <Paper shadow="xs" p="sm">
      <Stack gap="xs">
        {scoreboard?.timeLines && Object.keys(scoreboard.timeLines).length > 1 && (
          <Select
            defaultValue="all"
            data={[
              { value: 'all', label: t('game.label.score_table.all_teams') },
              ...Object.keys(scoreboard.timeLines)
                .filter((k) => k !== 'all')
                .map((o) => ({
                  value: o,
                  label: o === 'all' ? t('game.label.score_table.rank_total') : o,
                })),
            ]}
            value={division}
            onChange={(div) => {
              setDivision(div)
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
            <Table.Thead className={classes.thead}>
              <Table.Tr>
                {[
                  t('game.label.score_table.rank_total'),
                  t('game.label.score_table.team'),
                  t('game.label.score_table.score_total'),
                ].map((header, idx) => (
                  <Table.Th key={idx} className={cx(classes.left, classes.theadHeader)}>
                    {header}
                  </Table.Th>
                ))}
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
