import { Avatar, Box, Group, Input, Pagination, Paper, Select, Stack, Table } from '@mantine/core'
import React, { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router-dom'
import MobileScoreboardItemModal from '@Components/MobileScoreboardItemModal'
import { ScoreboardProps, useScoreboardStyles } from '@Components/ScoreboardTable'
import { BloodBonus, useBonusLabels } from '@Utils/Shared'
import { useGameScoreboard } from '@Utils/useGame'
import { ScoreboardItem, SubmissionType } from '@Api'

const TableRow: FC<{
  item: ScoreboardItem
  onOpenDetail: () => void
}> = ({ item, onOpenDetail }) => {
  const { classes, cx, theme } = useScoreboardStyles()
  const solved = item.challenges?.filter((c) => c.type !== SubmissionType.Unaccepted)
  return (
    <Table.Tr>
      <Table.Td className={cx(classes.theadMono, classes.theadFixLeft)}>{item.rank}</Table.Td>
      <Table.Td className={cx(classes.theadFixLeft)}>
        <Group justify="left" gap={5} wrap="nowrap" onClick={onOpenDetail}>
          <Avatar
            alt="avatar"
            src={item.avatar}
            radius="xl"
            size={30}
            color={theme.primaryColor}
            style={{
              '&:hover': {
                cursor: 'pointer',
              },
            }}
          >
            {item.name?.slice(0, 1) ?? 'T'}
          </Avatar>
          <Input
            variant="unstyled"
            value={item.name}
            readOnly
            size="sm"
            style={{
              wrapper: {
                width: '100%',
              },

              input: {
                userSelect: 'none',

                '&:hover': {
                  cursor: 'pointer',
                },
              },
            }}
          />
        </Group>
      </Table.Td>
      <Table.Td className={cx(classes.theadMono, classes.theadFixLeft)}>
        {solved?.reduce((acc, cur) => acc + (cur?.score ?? 0), 0)}
      </Table.Td>
    </Table.Tr>
  )
}

const ITEM_COUNT_PER_PAGE = 10

const MobileScoreboardTable: FC<ScoreboardProps> = ({ organization, setOrganization }) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const [activePage, setPage] = useState(1)
  const [bloodBonus, setBloodBonus] = useState(BloodBonus.default)
  const { classes, cx } = useScoreboardStyles()

  const { scoreboard } = useGameScoreboard(numId)

  const filtered =
    organization === 'all'
      ? scoreboard?.items
      : scoreboard?.items?.filter((s) => s.organization === organization)

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
              { value: 'all', label: t('game.label.score_table.rank_total') },
              ...Object.keys(scoreboard.timeLines)
                .filter((k) => k !== 'all')
                .map((o) => ({
                  value: o,
                  label: o === 'all' ? t('game.label.score_table.rank_total') : o,
                })),
            ]}
            value={organization}
            onChange={(org) => {
              setOrganization(org)
              setPage(1)
            }}
          />
        )}
        <Box pos="relative">
          <Box
            maw="100%"
            sx={{
              overflow: 'scroll',
              '::-webkit-scrollbar': {
                height: 0,
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
                    <Table.Th key={idx} className={cx(classes.theadFixLeft, classes.theadHeader)}>
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
        </Box>
        <Group justify="center">
          <Pagination
            size="md"
            value={activePage}
            onChange={setPage}
            total={Math.ceil((filtered?.length ?? 1) / ITEM_COUNT_PER_PAGE)}
            boundaries={1}
          />
        </Group>
      </Stack>
      <MobileScoreboardItemModal
        challenges={scoreboard?.challenges}
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

export default MobileScoreboardTable
